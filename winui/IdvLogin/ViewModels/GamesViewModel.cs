// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdvLogin.Models;
using IdvLogin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdvLogin.ViewModels;

public partial class GamesViewModel : ObservableObject
{
    private readonly GameService _gameService;

    public ObservableCollection<Game> Games { get; } = [];

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private string _statusMessage = "";

    public GamesViewModel()
    {
        _gameService = App.Services.GetRequiredService<GameService>();
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Games.Clear();
        foreach (var g in _gameService.Games.OrderByDescending(g => g.LastUsedTime))
            Games.Add(g);
    }

    [RelayCommand]
    private void StartGame()
    {
        if (SelectedGame is null) return;
        var ok = _gameService.StartGame(SelectedGame.GameId);
        StatusMessage = ok ? $"已启动：{SelectedGame.DisplayName}" : "启动失败，请检查游戏路径。";
    }

    [RelayCommand]
    private void RemoveGame()
    {
        if (SelectedGame is null) return;
        var name = SelectedGame.DisplayName;
        _gameService.Remove(SelectedGame.GameId);
        StatusMessage = $"已删除：{name}";
        Refresh();
    }

    /// <summary>从 UI 添加一个新游戏条目</summary>
    [RelayCommand]
    private void AddGame(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) return;
        var baseName = Path.GetFileNameWithoutExtension(exePath);
        // 生成唯一 GameId：若同名已存在则加上时间戳后缀
        var gameId = baseName;
        if (_gameService.GetGame(gameId) is not null)
            gameId = $"{baseName}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var game = new Game
        {
            GameId = gameId,
            Name   = baseName,
            Path   = exePath,
        };
        _gameService.AddOrUpdate(game);
        Refresh();
    }

    /// <summary>保存对所选游戏的修改</summary>
    [RelayCommand]
    private void SaveGame()
    {
        if (SelectedGame is null) return;
        _gameService.AddOrUpdate(SelectedGame);
        StatusMessage = "已保存。";
    }
}
