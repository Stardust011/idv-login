// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using IdvLogin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace IdvLogin.Views;

public sealed partial class ChannelsPage : Page
{
    public ChannelsViewModel ViewModel { get; } = new ChannelsViewModel();

    public ChannelsPage()
    {
        InitializeComponent();
    }
}
