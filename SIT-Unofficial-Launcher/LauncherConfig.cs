using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SIT_Unofficial_Launcher
{
    internal class LauncherConfig : PropertyChangedBase
    {
        //private ObservableCollection<Server> _servers = new();
        //public ObservableCollection<Server> Servers
        //{
        //    get => _servers;
        //    set => SetField(ref _servers, value);
        //}
        private string _lastServer = "http://127.0.0.1:6969";
        public string LastServer
        {
            get => _lastServer;
            set => SetField(ref _lastServer, value);
        }
        private string _username;
        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }
        private string _password;
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }
        private string _installPath;
        public string InstallPath
        {
            get => _installPath;
            set => SetField(ref _installPath, value);
        }
        private bool _rememberLogin = false;
        public bool RememberLogin
        {
            get => _rememberLogin;
            set => SetField(ref _rememberLogin, value);
        }
        private bool _automaticallyInstallSIT = true;
        public bool AutomaticallyInstallSIT
        {
            get => _automaticallyInstallSIT;
            set => SetField(ref _automaticallyInstallSIT, value);
        }
        private bool _installReflectionBinaries = true;
        public bool InstallReflectionBinaries
        {
            get => _installReflectionBinaries;
            set => SetField(ref _installReflectionBinaries, value);
        }
        private bool _closeAfterLaunch = false;
        public bool CloseAfterLaunch
        {
            get => _closeAfterLaunch;
            set => SetField(ref _closeAfterLaunch, value);
        }
        private string _tarkovVersion;
        public string TarkovVersion
        {
            get => _tarkovVersion;
            set => SetField(ref _tarkovVersion, value);
        }
        private bool _lookForUpdates = false;
        public bool LookForUpdates
        {
            get => _lookForUpdates;
            set => SetField(ref _lookForUpdates, value);
        }

        public static LauncherConfig Load()
        {
            LauncherConfig config = new();

            string currentDir = Directory.GetCurrentDirectory();

            if (File.Exists(currentDir + @"\LauncherConfig.json"))
                config = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(currentDir + @"\LauncherConfig.json"));

            return config;
        }

        public void Save(LauncherConfig launcherConfig, bool SaveAccount = false)
        {
            if (SaveAccount == false)
            {
                LauncherConfig newLauncherConfig = (LauncherConfig)launcherConfig.MemberwiseClone();
                newLauncherConfig.Username = null;
                newLauncherConfig.Password = null;

                File.WriteAllText("LauncherConfig.json", JsonSerializer.Serialize(newLauncherConfig, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
                File.WriteAllText("LauncherConfig.json", JsonSerializer.Serialize(launcherConfig, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public class Server : PropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }
        private string _address;
        public string Address
        {
            get => _address;
            set => SetField(ref _address, value);
        }
    }

    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value,
        [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
