// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdvLogin.Models;
using IdvLogin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdvLogin.ViewModels;

public partial class ChannelsViewModel : ObservableObject
{
    private readonly ChannelService   _channelService;
    private readonly CloudSyncService _syncService;

    public ObservableCollection<Channel> Channels { get; } = [];

    [ObservableProperty]
    private Channel? _selectedChannel;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _syncMasterKey = "";

    [ObservableProperty]
    private bool _isBusy;

    public ChannelsViewModel()
    {
        _channelService = App.Services.GetRequiredService<ChannelService>();
        _syncService    = App.Services.GetRequiredService<CloudSyncService>();

        _channelService.OnChannelsUpdated += (_, _) => Refresh();
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Channels.Clear();
        foreach (var ch in _channelService.Channels.OrderByDescending(c => c.LastLoginTime))
            Channels.Add(ch);
    }

    [RelayCommand]
    private void DeleteChannel()
    {
        if (SelectedChannel is null) return;
        var name = SelectedChannel.Name;
        _channelService.Delete(SelectedChannel.Uuid);
        StatusMessage = $"已删除账号：{name}";
    }

    [RelayCommand]
    private void RenameChannel(string newName)
    {
        if (SelectedChannel is null || string.IsNullOrWhiteSpace(newName)) return;
        _channelService.Rename(SelectedChannel.Uuid, newName);
        StatusMessage = $"已重命名为：{newName}";
        Refresh();
    }

    [RelayCommand]
    private async Task PushToCloudAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncMasterKey)) { StatusMessage = "请输入主密钥。"; return; }
        IsBusy = true;
        try
        {
            await _syncService.PushAsync(SyncMasterKey);
            StatusMessage = "云端同步推送完成。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"推送失败：{ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PullFromCloudAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncMasterKey)) { StatusMessage = "请输入主密钥。"; return; }
        IsBusy = true;
        try
        {
            var ok = await _syncService.PullAsync(SyncMasterKey);
            StatusMessage = ok ? "云端同步拉取完成。" : "拉取失败，请检查主密钥。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"拉取失败：{ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
