using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using SIT_Unofficial_Launcher.Views;
using System;
using System.Diagnostics;

namespace SIT_Unofficial_Launcher.CustomControls
{
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        private void CloseButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void MinimizeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.WindowState = WindowState.Minimized;
            }
        }

        private void OnHelpButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/Lacyway/SIT-Unofficial-Launcher/tree/master#setting-up-sit");
        }
    }
}
