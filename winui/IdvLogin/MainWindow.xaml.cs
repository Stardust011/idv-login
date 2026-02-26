// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using IdvLogin.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdvLogin;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // 默认显示游戏管理页
        ContentFrame.Navigate(typeof(GamesPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "games"    => typeof(GamesPage),
                "channels" => typeof(ChannelsPage),
                "settings" => typeof(SettingsPage),
                "about"    => typeof(AboutPage),
                _          => typeof(GamesPage),
            };
            ContentFrame.Navigate(pageType);
        }
    }
}
