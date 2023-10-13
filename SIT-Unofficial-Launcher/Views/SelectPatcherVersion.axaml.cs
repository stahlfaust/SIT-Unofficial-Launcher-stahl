using Avalonia.Controls;
using Avalonia.Interactivity;
using SIT_Unofficial_Launcher.Classes;
using System.Collections.Generic;

namespace SIT_Unofficial_Launcher.Views
{
    public partial class SelectPatcherVersion : Window
    {
        public SelectPatcherVersion()
        {
            InitializeComponent();
        }

        public SelectPatcherVersion(List<GiteaRelease> releases, string version)
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
            else
                Close(null);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
