// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Text.Json;
using IdvLogin.Models;

namespace IdvLogin.Services;

/// <summary>
/// 云端资源管理（对应 Python 版 cloudRes.py）
/// 从 CDN 拉取并本地缓存 cloudRes.json
/// </summary>
public class CloudResService
{
    private static readonly string[] CloudUrls =
    [
        "https://gitee.com/opguess/idv-login/raw/main/assets/cloudRes.json",
        "https://cdn.jsdelivr.net/gh/Alexander-Porter/idv-login@main/assets/cloudRes.json",
    ];

    private readonly AppConfig _config;
    private CloudResData _data = new();

    public CloudResService(AppConfig config)
    {
        _config = config;
        LoadCache();
    }

    private void LoadCache()
    {
        try
        {
            var path = _config.CloudResCacheFile;
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                _data = JsonSerializer.Deserialize<CloudResData>(text) ?? new CloudResData();
            }
        }
        catch { }
    }

    /// <summary>
    /// 若云端版本比本地新则下载并更新缓存
    /// </summary>
    public async Task UpdateCacheIfNeededAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Add("User-Agent", "IdvLogin/WinUI3");

        foreach (var url in CloudUrls)
        {
            try
            {
                var json = await http.GetStringAsync(url);
                var cloud = JsonSerializer.Deserialize<CloudResData>(json);
                if (cloud is null) continue;

                if (cloud.LastModified > _data.LastModified)
                {
                    _data = cloud;
                    File.WriteAllText(_config.CloudResCacheFile,
                        JsonSerializer.Serialize(cloud, new JsonSerializerOptions { WriteIndented = true }));
                }
                return;
            }
            catch { }
        }
    }

    // ─── 查询方法 ────────────────────────────────────────────────────────────

    public CloudResEntry? GetChannelData(string channelName, string shortGameId) =>
        _data.Data.FirstOrDefault(d =>
            d.AppChannel == channelName &&
            GetShortId(d.GameId) == shortGameId);

    public CloudResEntry? GetByGameId(string shortGameId) =>
        _data.Data.FirstOrDefault(d => GetShortId(d.GameId) == shortGameId);

    public List<CloudResEntry> GetAllByGameId(string shortGameId) =>
        _data.Data.Where(d => GetShortId(d.GameId) == shortGameId).ToList();

    public CloudResFeature? GetFeatureByGameId(string shortGameId) =>
        _data.FeatureGameShortIds.FirstOrDefault(f => GetShortId(f.GameId) == shortGameId);

    public string GetStartArgument(string shortGameId) =>
        GetFeatureByGameId(shortGameId)?.StartArgument ?? "";

    public bool IsConvertToNormal(string shortGameId) =>
        GetFeatureByGameId(shortGameId)?.ConvertToNormal ?? false;

    public bool IsGameInQrCodeLoginList(string gameId)
    {
        var shortId = GetShortId(gameId);
        return _data.NeteaseQrCodeLoginGameList.Any(g => GetShortId(g.GameId) == shortId);
    }

    public string? GetQrCodeAppChannel(string gameId)
    {
        var shortId = GetShortId(gameId);
        return _data.NeteaseQrCodeLoginGameList
            .FirstOrDefault(g => GetShortId(g.GameId) == shortId)?.AppChannel;
    }

    public string GetAnnouncement() => _data.Announcement;
    public string GetDownloadUrl()  => _data.DownloadUrl;
    public string GetGuideUrl()     => _data.GuideUrl;
    public string GetVersion()      => _data.Version;
    public bool   IsCriticalUpdate() => _data.CriticalUpdate;

    /// <summary>从完整 game_id 取短 id（最后一段 "-" 之后）</summary>
    public static string GetShortId(string gameId)
    {
        if (string.IsNullOrEmpty(gameId)) return gameId;
        var idx = gameId.LastIndexOf('-');
        return idx >= 0 ? gameId[(idx + 1)..] : gameId;
    }
}
