// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdvLogin.Models;
using Konscious.Security.Cryptography;

namespace IdvLogin.Services;

/// <summary>
/// 云端账号同步服务（对应 Python 版 cloudSync.py）
/// 使用 WebNote API 存储 AES-256-GCM 加密的账号数据
/// 主密钥通过 Argon2id 派生笔记 ID 和加密密钥
/// </summary>
public class CloudSyncService
{
    private const string BaseUrl      = "https://api.txttool.cn/netcut/note";
    private const string SyncTitle    = "sync_data";

    // Argon2id 参数（与 Python 版保持一致）
    private const int Argon2TimeCost    = 3;
    private const int Argon2MemoryKib   = 262144;
    private const int Argon2Parallelism = 2;

    private static readonly byte[] SaltNoteId       = Encoding.UTF8.GetBytes("idv-login/cloud-sync/note-id/v2");
    private static readonly byte[] SaltNotePassword = Encoding.UTF8.GetBytes("idv-login/cloud-sync/note-password/v2");

    private readonly AppConfig      _config;
    private readonly ChannelService _channelService;

    public CloudSyncService(AppConfig config, ChannelService channelService)
    {
        _config         = config;
        _channelService = channelService;
    }

    // ─── 主密钥派生 ───────────────────────────────────────────────────────────

    public string DeriveNoteId(string masterKey)       => DeriveHex(masterKey, SaltNoteId,       16);
    public string DeriveNotePassword(string masterKey) => DeriveHex(masterKey, SaltNotePassword, 32);

    private static string DeriveHex(string masterKey, byte[] salt, int dkLen)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(masterKey))
        {
            Salt            = salt,
            Iterations      = Argon2TimeCost,
            MemorySize      = Argon2MemoryKib,
            DegreeOfParallelism = Argon2Parallelism,
        };
        return Convert.ToHexString(argon2.GetBytes(dkLen)).ToLowerInvariant();
    }

    // ─── AES-GCM 加密 / 解密 ─────────────────────────────────────────────────

    private static byte[] Encrypt(string plainText, byte[] key)
    {
        var nonce      = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipher     = new byte[plainBytes.Length];
        var tag        = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        // 格式：[12-byte nonce][16-byte tag][cipher]
        var result = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce,  0, result,  0, 12);
        Buffer.BlockCopy(tag,    0, result, 12, 16);
        Buffer.BlockCopy(cipher, 0, result, 28, cipher.Length);
        return result;
    }

    private static string Decrypt(byte[] data, byte[] key)
    {
        var nonce  = data[..12];
        var tag    = data[12..28];
        var cipher = data[28..];
        var plain  = new byte[cipher.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    // ─── WebNote API 封装 ────────────────────────────────────────────────────

    private async Task<JsonDocument?> CallApiAsync(
        HttpClient http, string path, Dictionary<string, string> form)
    {
        var content  = new FormUrlEncodedContent(form);
        var response = await http.PostAsync($"{BaseUrl}/{path}/", content);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text);
    }

    // ─── 公开操作 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 将本地账号数据加密后推送到云端
    /// </summary>
    public async Task PushAsync(string masterKey, int expireTime = 259200)
    {
        var noteId       = DeriveNoteId(masterKey);
        var notePassword = DeriveNotePassword(masterKey);
        var encKey       = Convert.FromHexString(DeriveHex(masterKey,
            Encoding.UTF8.GetBytes("idv-login/cloud-sync/enc-key/v2"), 32));

        var plain = JsonSerializer.Serialize(_channelService.Channels);
        var enc   = Encrypt(plain, encKey);
        var payload = JsonSerializer.Serialize(new
        {
            title   = SyncTitle,
            content = Convert.ToBase64String(enc),
        });

        using var http = CreateHttpClient();

        // 先查是否已有笔记
        try
        {
            var info = await CallApiAsync(http, "info",
                new() { ["note_name"] = noteId, ["note_pwd"] = notePassword });
            var data = info?.RootElement.GetProperty("data");
            var noteInternalId = data?.GetProperty("note_id").GetString() ?? "";
            var noteToken      = data?.GetProperty("note_token").GetString() ?? "";

            if (!string.IsNullOrEmpty(noteInternalId))
            {
                // 更新已有笔记
                await CallApiAsync(http, "save", new()
                {
                    ["note_name"]    = noteId,
                    ["note_id"]      = noteInternalId,
                    ["note_content"] = payload,
                    ["note_token"]   = noteToken,
                    ["expire_time"]  = expireTime.ToString(),
                    ["note_pwd"]     = notePassword,
                });
                return;
            }
        }
        catch { }

        // 创建新笔记（两阶段创建，先无密码，再有密码）
        var created = await CallApiAsync(http, "save", new()
        {
            ["note_name"]    = noteId,
            ["note_id"]      = "",
            ["note_content"] = JsonSerializer.Serialize(new object[] { new { title = "sync_placeholder", content = "pending" } }),
            ["note_token"]   = "",
            ["expire_time"]  = expireTime.ToString(),
            ["note_pwd"]     = "",
        });

        var createdData    = created?.RootElement.GetProperty("data");
        var newInternalId  = createdData?.GetProperty("note_id").GetString()  ?? throw new InvalidOperationException("创建笔记失败");
        var newToken       = createdData?.GetProperty("note_token").GetString() ?? throw new InvalidOperationException("创建笔记失败");

        await CallApiAsync(http, "save", new()
        {
            ["note_name"]    = noteId,
            ["note_id"]      = newInternalId,
            ["note_content"] = payload,
            ["note_token"]   = newToken,
            ["expire_time"]  = expireTime.ToString(),
            ["note_pwd"]     = notePassword,
        });
    }

    /// <summary>
    /// 从云端拉取账号数据并解密后合并到本地
    /// </summary>
    public async Task<bool> PullAsync(string masterKey)
    {
        var noteId       = DeriveNoteId(masterKey);
        var notePassword = DeriveNotePassword(masterKey);
        var encKey       = Convert.FromHexString(DeriveHex(masterKey,
            Encoding.UTF8.GetBytes("idv-login/cloud-sync/enc-key/v2"), 32));

        using var http = CreateHttpClient();
        var doc = await CallApiAsync(http, "info",
            new() { ["note_name"] = noteId, ["note_pwd"] = notePassword });
        if (doc is null) return false;

        var content = doc.RootElement
            .GetProperty("data")
            .GetProperty("note_content")
            .GetString();
        if (string.IsNullOrEmpty(content)) return false;

        var outer   = JsonDocument.Parse(content);
        if (!outer.RootElement.TryGetProperty("content", out var encProp))
            return false;
        var encB64  = encProp.GetString() ?? "";
        if (string.IsNullOrEmpty(encB64)) return false;
        var encData = Convert.FromBase64String(encB64);
        var plain   = Decrypt(encData, encKey);

        var remote = JsonSerializer.Deserialize<List<Channel>>(plain) ?? [];
        MergeChannels(remote);
        return true;
    }

    private void MergeChannels(IEnumerable<Channel> remote)
    {
        // TODO: 实现双向合并逻辑（以 uuid 为键，取 LastLoginTime 更新的一方）
        foreach (var r in remote)
        {
            var existing = _channelService.FindByUuid(r.Uuid);
            if (existing is null)
                _channelService.ImportFromScan(r.LoginInfo, new Dictionary<string, object?>
                {
                    ["user"]     = r.UserInfo,
                    ["ext_info"] = r.ExtInfo,
                    ["device"]   = r.DeviceInfo,
                });
            else if (r.LastLoginTime > existing.LastLoginTime)
            {
                existing.LoginInfo  = r.LoginInfo;
                existing.UserInfo   = r.UserInfo;
                existing.ExtInfo    = r.ExtInfo;
                existing.DeviceInfo = r.DeviceInfo;
                existing.LastLoginTime = r.LastLoginTime;
            }
        }
        _channelService.Save();
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        http.DefaultRequestHeaders.Referrer = new Uri("https://webnote.cc/");
        return http;
    }
}
