// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using IdvLogin.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace IdvLogin.Views;

public sealed partial class GamesPage : Page
{
    public GamesViewModel ViewModel { get; } = new GamesViewModel();

    public GamesPage()
    {
        InitializeComponent();
    }

    private async void BrowseExe_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");

        // WinUI3：需要将 picker 与当前窗口绑定
        var window = (App.Current as App)?.MainAppWindow;
        if (window is null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ExePathBox.Text = file.Path;
            ViewModel.AddGameCommand.Execute(file.Path);
        }
    }
}
