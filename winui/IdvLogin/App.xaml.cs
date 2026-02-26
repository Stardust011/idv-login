// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using IdvLogin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace IdvLogin;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>对主窗口的引用，供需要绑定 HWND 的 picker/对话框使用</summary>
    public MainWindow? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 核心服务
        services.AddSingleton<AppConfig>();
        services.AddSingleton<CloudResService>();
        services.AddSingleton<CertService>();
        services.AddSingleton<GameService>();
        services.AddSingleton<ChannelService>();
        services.AddSingleton<CloudSyncService>();
        services.AddSingleton<ProxyService>();

        return services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 确保以管理员身份运行
        if (!AdminHelper.IsAdmin())
        {
            AdminHelper.RelaunchAsAdmin();
            Current.Exit();
            return;
        }

        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();

        // 后台初始化
        var cloudRes = Services.GetRequiredService<CloudResService>();
        await cloudRes.UpdateCacheIfNeededAsync();
    }
}
