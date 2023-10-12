using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SIT_Unofficial_Launcher.Views
{
    public partial class SelectSitVersion : Window
    {
        public SelectSitVersion()
        {
            InitializeComponent();
        }

        public SelectSitVersion(List<GithubRelease> releases, string version)
            : this()
        {
            ReleasesCombo.DataContext = releases;
            ReleasesCombo.ItemsSource = releases;
            ReleasesCombo.SelectedIndex = 0;
            VersionText.Text = "Current Tarkov version: " + version;
        }

        private void OnInstallClick(object sender, RoutedEventArgs e)
        {
            if (ReleasesCombo.SelectedItem != null)
                Close(ReleasesCombo.SelectedItem);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
