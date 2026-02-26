// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Text.Json.Serialization;

namespace IdvLogin.Models;

/// <summary>
/// 来自云端的单个渠道配置条目
/// </summary>
public class CloudResEntry
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("app_channel")]
    public string AppChannel { get; set; } = "";

    [JsonPropertyName("package_name")]
    public string PackageName { get; set; } = "";

    [JsonPropertyName("log_key")]
    public string LogKey { get; set; } = "";

    /// <summary>原始字段透传（允许有未知字段）</summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extra { get; set; }
}

/// <summary>
/// feature_game_short_ids 条目
/// </summary>
public class CloudResFeature
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("start_argument")]
    public string StartArgument { get; set; } = "";

    [JsonPropertyName("downloadable")]
    public bool Downloadable { get; set; } = true;

    [JsonPropertyName("convert_to_normal")]
    public bool ConvertToNormal { get; set; }

    [JsonPropertyName("download_distributions")]
    public List<object?>? DownloadDistributions { get; set; }
}

/// <summary>
/// 二维码扫码登录支持的游戏条目
/// </summary>
public class QrCodeLoginGame
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("app_channel")]
    public string AppChannel { get; set; } = "";
}

/// <summary>
/// cloudRes.json 根对象
/// </summary>
public class CloudResData
{
    [JsonPropertyName("lastModified")]
    public long LastModified { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("announcement")]
    public string Announcement { get; set; } = "";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("guideUrl")]
    public string GuideUrl { get; set; } = "";

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = "";

    [JsonPropertyName("detail_html")]
    public string? DetailHtml { get; set; }

    [JsonPropertyName("critical_update")]
    public bool CriticalUpdate { get; set; }

    [JsonPropertyName("data")]
    public List<CloudResEntry> Data { get; set; } = [];

    [JsonPropertyName("feature_game_short_ids")]
    public List<CloudResFeature> FeatureGameShortIds { get; set; } = [];

    [JsonPropertyName("netease_qrcode_login_game_list")]
    public List<QrCodeLoginGame> NeteaseQrCodeLoginGameList { get; set; } = [];
}
