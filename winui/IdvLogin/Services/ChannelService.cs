// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IdvLogin.Models;

namespace IdvLogin.Services;

/// <summary>
/// 渠道服账号管理服务（对应 Python 版 channelmgr.py）
/// 负责账号记录的持久化、登录模拟、账号扫码导入等
/// </summary>
public class ChannelService
{
    private readonly AppConfig _config;
    private readonly CloudResService _cloudRes;

    private List<Channel> _channels = [];

    public IReadOnlyList<Channel> Channels => _channels.AsReadOnly();

    public ChannelService(AppConfig config, CloudResService cloudRes)
    {
        _config   = config;
        _cloudRes = cloudRes;
        Load();
    }

    // ─── 持久化 ───────────────────────────────────────────────────────────────

    private void Load()
    {
        try
        {
            if (!File.Exists(_config.ChannelRecord)) return;
            var text = File.ReadAllText(_config.ChannelRecord);
            _channels = JsonSerializer.Deserialize<List<Channel>>(text) ?? [];
        }
        catch { _channels = []; }
    }

    public void Save()
    {
        File.WriteAllText(_config.ChannelRecord,
            JsonSerializer.Serialize(_channels, new JsonSerializerOptions { WriteIndented = true }));
        OnChannelsUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>账号记录变更时触发（供 ViewModel 刷新 UI 使用）</summary>
    public event EventHandler? OnChannelsUpdated;

    // ─── 账号查询 ─────────────────────────────────────────────────────────────

    public Channel? FindByUuid(string uuid) =>
        _channels.FirstOrDefault(c => c.Uuid == uuid);

    /// <summary>
    /// 列出指定游戏可用的账号（空 gameId 返回全部）
    /// </summary>
    public IEnumerable<object> ListChannels(string gameId = "")
    {
        return _channels
            .Where(c => string.IsNullOrEmpty(gameId) || c.CrossGames ||
                        CloudResService.GetShortId(
                            c.LoginInfo.TryGetValue("game_id", out var gid) ? gid?.ToString() ?? "" : "")
                        == CloudResService.GetShortId(gameId))
            .OrderByDescending(c => c.LastLoginTime)
            .Select(c => c.GetSummary());
    }

    // ─── 账号操作 ─────────────────────────────────────────────────────────────

    public bool Rename(string uuid, string newName)
    {
        var ch = FindByUuid(uuid);
        if (ch is null) return false;
        ch.Name = newName;
        Save();
        return true;
    }

    public bool Delete(string uuid)
    {
        var idx = _channels.FindIndex(c => c.Uuid == uuid);
        if (idx < 0) return false;
        _channels.RemoveAt(idx);
        Save();
        return true;
    }

    /// <summary>
    /// 从扫码结果导入账号
    /// </summary>
    public bool ImportFromScan(Dictionary<string, object?> loginInfo,
                               Dictionary<string, object?> exchangeInfo)
    {
        var ch = new Channel
        {
            LoginInfo  = loginInfo,
            UserInfo   = GetDict(exchangeInfo, "user"),
            ExtInfo    = GetDict(exchangeInfo, "ext_info"),
            DeviceInfo = GetDict(exchangeInfo, "device"),
            CrossGames = true,
        };

        ch.Uuid = $"{ch.ChannelName}-{loginInfo.TryGetValue("code", out var code) && code is not null ? code : loginInfo.TryGetValue("uid", out var uid) && uid is not null ? uid : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()}";
        ch.Name = ch.Uuid;

        // 合并重复账号（相同 user_info.id）
        if (ch.UserInfo.TryGetValue("id", out var userId) && userId is not null)
        {
            var dups = _channels
                .Where(c => c.UserInfo.TryGetValue("id", out var id) && id?.ToString() == userId.ToString())
                .OrderByDescending(c => c.LastLoginTime)
                .ToList();
            if (dups.Count > 0)
            {
                ch.Name = dups[0].Name;
                ch.Uuid = dups[0].Uuid;
                ch.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                foreach (var dup in dups) _channels.Remove(dup);
            }
        }

        _channels.Add(ch);
        Save();
        return true;
    }

    /// <summary>
    /// 模拟扫码（向 mkey 服务器发送扫码请求）
    /// </summary>
    public async Task<object?> SimulateScanAsync(string uuid, string scannerUuid, string gameId)
    {
        var ch = FindByUuid(uuid);
        if (ch is null) return null;

        // 特殊模式：直接返回 UniSDK 数据
        if (scannerUuid == "Kinich")
            return ch.GetUniSdkData(gameId);

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Dalvik/2.1.0 (Linux; U; Android 12; M2102K1AC Build/V417IR)");

        var query = new Dictionary<string, string>
        {
            ["uuid"]          = scannerUuid,
            ["login_channel"] = ch.ChannelName,
            ["app_channel"]   = ch.ChannelName,
            ["pay_channel"]   = ch.ChannelName,
            ["game_id"]       = gameId,
            ["gv"]            = "157",
            ["gvn"]           = "1.5.80",
            ["cv"]            = "a1.5.0",
        };

        try
        {
            var qs = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var resp = await http.GetAsync(
                $"https://service.mkey.163.com/mpay/api/qrcode/scan?{qs}");
            if (!resp.IsSuccessStatusCode) return null;

            var result = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();

            // 发烧平台特殊处理：code 1424 表示需要换 game_id 重试
            if (result?.TryGetValue("code", out var code) == true &&
                code?.ToString() == "1424" &&
                result.TryGetValue("game", out var gamePart) &&
                gamePart is System.Text.Json.JsonElement ge &&
                ge.TryGetProperty("id", out var newId))
            {
                query["game_id"] = newId.GetString() ?? gameId;
                qs = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                resp = await http.GetAsync($"https://service.mkey.163.com/mpay/api/qrcode/scan?{qs}");
                if (!resp.IsSuccessStatusCode) return null;
            }

            return await SimulateConfirmAsync(ch, scannerUuid, query["game_id"]);
        }
        catch { return null; }
    }

    /// <summary>
    /// 模拟确认（向 mkey 服务器发送确认请求）
    /// </summary>
    public async Task<object?> SimulateConfirmAsync(Channel ch, string scannerUuid, string gameId)
    {
        var data = ch.GetUniSdkData(gameId);
        data["uuid"]    = scannerUuid;
        data["game_id"] = gameId;

        var body = string.Join("&",
            data.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value?.ToString() ?? "")}"));

        using var http = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        var resp = await http.PostAsync(
            "https://service.mkey.163.com/mpay/api/qrcode/confirm_login", content);

        if (!resp.IsSuccessStatusCode) return null;

        ch.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Save();
        return await resp.Content.ReadFromJsonAsync<object>();
    }

    // ─── 工具方法 ─────────────────────────────────────────────────────────────

    private static Dictionary<string, object?> GetDict(
        Dictionary<string, object?> source, string key)
    {
        if (source.TryGetValue(key, out var val) &&
            val is System.Text.Json.JsonElement je &&
            je.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText())
                   ?? [];
        }
        return [];
    }
}
