// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Text.Json.Serialization;

namespace IdvLogin.Models;

/// <summary>
/// 渠道服账号登录凭证基类
/// </summary>
public class Channel
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("login_info")]
    public Dictionary<string, object?> LoginInfo { get; set; } = [];

    [JsonPropertyName("user_info")]
    public Dictionary<string, object?> UserInfo { get; set; } = [];

    [JsonPropertyName("ext_info")]
    public Dictionary<string, object?> ExtInfo { get; set; } = [];

    [JsonPropertyName("device_info")]
    public Dictionary<string, object?> DeviceInfo { get; set; } = [];

    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("last_login_time")]
    public long LastLoginTime { get; set; }

    /// <summary>
    /// 是否跨游戏通用（官服账号可跨游；渠道服账号通常绑定特定游戏）
    /// </summary>
    [JsonPropertyName("cross_games")]
    public bool CrossGames { get; set; } = true;

    /// <summary>
    /// 登录渠道名，从 LoginInfo["login_channel"] 读取
    /// </summary>
    [JsonIgnore]
    public string ChannelName => LoginInfo.TryGetValue("login_channel", out var v) ? v?.ToString() ?? "" : "";

    /// <summary>
    /// 获取非敏感的展示数据
    /// </summary>
    public object GetSummary() => new
    {
        uuid         = Uuid,
        name         = Name,
        create_time  = CreateTime,
        last_login_time = LastLoginTime,
        channel_name = ChannelName,
    };

    // UniSDK 版本标识（随游戏更新调整）
    private const string GV  = "157";
    private const string GVN = "1.5.80";
    private const string CV  = "a1.5.0";

    /// <summary>
    /// 获取 UniSDK 登录体（传给游戏服务端用）
    /// </summary>
    public Dictionary<string, object?> GetUniSdkData(string gameId = "")
    {
        return new Dictionary<string, object?>
        {
            ["user_id"]       = UserInfo.TryGetValue("id", out var id) ? id : "",
            ["token"]         = UserInfo.TryGetValue("token", out var tok) ? tok : "",
            ["login_channel"] = ExtInfo.TryGetValue("src_app_channel2", out var ch2) ? ch2 : "",
            ["udid"]          = ExtInfo.TryGetValue("src_udid", out var udid) ? udid : "",
            ["app_channel"]   = ExtInfo.TryGetValue("src_app_channel", out var ch) ? ch : "",
            ["sdk_version"]   = ExtInfo.TryGetValue("src_jf_game_id", out var jf) ? jf : "",
            ["jf_game_id"]    = ExtInfo.TryGetValue("src_jf_game_id", out var jf2) ? jf2 : "",
            ["pay_channel"]   = ExtInfo.TryGetValue("src_pay_channel", out var pay) ? pay : "",
            ["extra_data"]    = "",
            ["extra_unisdk_data"] = ExtInfo.TryGetValue("extra_unisdk_data", out var ex) ? ex : "",
            ["gv"]            = GV,
            ["gvn"]           = GVN,
            ["cv"]            = CV,
        };
    }
}
