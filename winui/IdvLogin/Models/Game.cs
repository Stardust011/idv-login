// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace IdvLogin.Models;

/// <summary>
/// 游戏安装信息
/// </summary>
public class Game
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 游戏可执行文件路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("should_auto_start")]
    public bool ShouldAutoStart { get; set; }

    [JsonPropertyName("auto_close_after_login")]
    public bool AutoCloseAfterLogin { get; set; } = true;

    /// <summary>
    /// 登录等待延迟（秒）
    /// </summary>
    [JsonPropertyName("login_delay")]
    public int LoginDelay { get; set; } = 6;

    [JsonPropertyName("last_used_time")]
    public long LastUsedTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("default_distribution")]
    public int DefaultDistribution { get; set; } = -1;

    /// <summary>
    /// 显示名（优先使用 Name，否则用 GameId）
    /// </summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Name) ? GameId : Name;

    /// <summary>
    /// 短 game_id（取 "-" 后最后一段，如 "idv-g37" -> "g37"）
    /// </summary>
    [JsonIgnore]
    public string ShortGameId
    {
        get
        {
            var parts = GameId.Split('-');
            return parts.Length > 0 ? parts[^1] : GameId;
        }
    }

    /// <summary>
    /// 启动游戏进程
    /// </summary>
    public bool Start(string? extraArgs = null)
    {
        if (string.IsNullOrEmpty(Path) || !File.Exists(Path))
            return false;

        try
        {
            var gameDir = System.IO.Path.GetDirectoryName(Path) ?? "";
            var psi = new ProcessStartInfo
            {
                FileName = Path,
                WorkingDirectory = gameDir,
                UseShellExecute = true,
            };
            if (!string.IsNullOrWhiteSpace(extraArgs))
                psi.Arguments = extraArgs;

            Process.Start(psi);
            LastUsedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
