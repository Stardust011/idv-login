// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Text.Json;
using IdvLogin.Models;

namespace IdvLogin.Services;

/// <summary>
/// 游戏安装管理服务（对应 Python 版 gamemgr.py）
/// 负责游戏列表的持久化与游戏启动
/// </summary>
public class GameService
{
    private readonly AppConfig _config;
    private readonly CloudResService _cloudRes;
    private readonly string _gamesFile;

    private List<Game> _games = [];

    public IReadOnlyList<Game> Games => _games.AsReadOnly();

    public GameService(AppConfig config, CloudResService cloudRes)
    {
        _config   = config;
        _cloudRes = cloudRes;
        _gamesFile = Path.Combine(config.WorkDirectory, "games.json");
        Load();
    }

    // ─── 持久化 ───────────────────────────────────────────────────────────────

    private void Load()
    {
        try
        {
            if (!File.Exists(_gamesFile)) return;
            var text = File.ReadAllText(_gamesFile);
            _games = JsonSerializer.Deserialize<List<Game>>(text) ?? [];
        }
        catch { _games = []; }
    }

    public void Save()
    {
        File.WriteAllText(_gamesFile,
            JsonSerializer.Serialize(_games, new JsonSerializerOptions { WriteIndented = true }));
    }

    // ─── 游戏管理 ─────────────────────────────────────────────────────────────

    public Game? GetGame(string gameId)
    {
        var shortId = CloudResService.GetShortId(gameId);
        return _games.FirstOrDefault(g =>
            CloudResService.GetShortId(g.GameId) == shortId);
    }

    public void AddOrUpdate(Game game)
    {
        var idx = _games.FindIndex(g =>
            CloudResService.GetShortId(g.GameId) ==
            CloudResService.GetShortId(game.GameId));
        if (idx >= 0) _games[idx] = game;
        else           _games.Add(game);
        Save();
    }

    public bool Remove(string gameId)
    {
        var idx = _games.FindIndex(g =>
            CloudResService.GetShortId(g.GameId) ==
            CloudResService.GetShortId(gameId));
        if (idx < 0) return false;
        _games.RemoveAt(idx);
        Save();
        return true;
    }

    // ─── 游戏启动 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 启动指定游戏（若配置了 ConvertToNormal 则附带启动参数）
    /// </summary>
    public bool StartGame(string gameId)
    {
        var game = GetGame(gameId);
        if (game is null) return false;

        string? args = null;
        if (_cloudRes.IsConvertToNormal(game.ShortGameId))
            args = _cloudRes.GetStartArgument(game.ShortGameId);

        return game.Start(args);
    }

    // ─── 工具方法 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 返回按最近使用时间降序排列的游戏列表摘要
    /// </summary>
    public IEnumerable<object> ListGames() =>
        _games
            .OrderByDescending(g => g.LastUsedTime)
            .Select(g => new
            {
                game_id        = g.GameId,
                name           = g.DisplayName,
                path           = g.Path,
                last_used_time = g.LastUsedTime,
                should_auto_start = g.ShouldAutoStart,
            });
}
