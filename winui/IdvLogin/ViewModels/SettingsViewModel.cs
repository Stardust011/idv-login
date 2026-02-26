// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdvLogin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdvLogin.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppConfig  _config;
    private readonly CertService _certService;

    [ObservableProperty]
    private bool _isCaCertInstalled;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public SettingsViewModel()
    {
        _config      = App.Services.GetRequiredService<AppConfig>();
        _certService = App.Services.GetRequiredService<CertService>();
        RefreshCertStatus();
    }

    [RelayCommand]
    private void RefreshCertStatus()
    {
        IsCaCertInstalled = _certService.IsCaInstalledInStore();
    }

    [RelayCommand]
    private void InstallCaCert()
    {
        IsBusy = true;
        try
        {
            if (!_certService.IsCaCertValid())
                _certService.GenerateCa();

            var ok = _certService.ImportCaToWindowsStore();
            StatusMessage = ok ? "CA 证书已成功安装到受信任根存储。" : "安装失败，请确认程序以管理员身份运行。";
            RefreshCertStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"安装出错：{ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void RegenerateCaCert()
    {
        IsBusy = true;
        try
        {
            _certService.GenerateCa();
            var ok = _certService.ImportCaToWindowsStore();
            StatusMessage = ok ? "CA 证书已重新生成并安装。" : "生成成功，但安装到系统存储失败。";
            RefreshCertStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"重新生成出错：{ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
