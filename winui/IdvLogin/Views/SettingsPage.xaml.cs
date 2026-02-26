// Copyright (c) 2026 Alexander-Porter
// Licensed under the GNU General Public License v3.0

using IdvLogin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace IdvLogin.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

    public SettingsPage()
    {
        InitializeComponent();
    }
}
