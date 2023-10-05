using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Dto;
using System.Collections.Generic;
using MsBox.Avalonia.Models;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Avalonia.Threading;
using System;
using System.Threading;
using System.Reflection;
using System.Text.Json;

namespace SIT_Unofficial_Launcher.Views;

public partial class MainWindow : Window
{
    LauncherConfig config = new();
    HttpClient httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    public MainWindow()
    {
        InitializeComponent();
        config = LauncherConfig.Load();
        DataContext = config;

        VersionText.Text = "Launcher Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

        if (config.LookForUpdates == true)
            LookForUpdate();        
    }

    private async void LookForUpdate()
    {
        string latestVersion = await httpClient.GetStringAsync("https://raw.githubusercontent.com/Lacyway/SIT-Unofficial-Launcher/master/Version.txt".Trim());
        if (latestVersion != Assembly.GetExecutingAssembly().GetName().Version.ToString())
        {
            ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Update Found", "New Update Available. Would you like to go to the download page?", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question).ShowWindowDialogAsync(this);
            if (result == ButtonResult.Yes)
            {
                Process.Start("explorer.exe", "https://github.com/Lacyway/SIT-Unofficial-Launcher/releases/latest");
            }
        }
    }

    private async Task CleanUpEFTDirectory()
    {
        StatusText.Text = "Removing files...";
        StatusText.IsVisible = true;
        StatusProgressBar.IsVisible = true;

        string battlEyeDir = config.InstallPath + @"\BattlEye";
        if (Directory.Exists(battlEyeDir))
        {
            Directory.Delete(battlEyeDir, true);
        }
        string battlEyeExe = config.InstallPath + @"\EscapeFromTarkov_BE.exe";
        if (File.Exists(battlEyeExe))
        {
            File.Delete(battlEyeExe);
        }
        string cacheDir = config.InstallPath + @"\cache";
        if (Directory.Exists(cacheDir))
        {
            Directory.Delete(cacheDir, true);
        }
        string consistencyPath = config.InstallPath + @"\ConsistencyInfo";
        if (File.Exists(consistencyPath))
        {
            File.Delete(consistencyPath);
        }
        string uninstallPath = config.InstallPath + @"\Uninstall.exe";
        if (File.Exists(uninstallPath))
        {
            File.Delete(uninstallPath);
        }
        string logsDirPath = config.InstallPath + @"\Logs";
        if (Directory.Exists(logsDirPath))
        {
            Directory.Delete(logsDirPath);
        }

        StatusText.IsVisible = false;
        StatusProgressBar.IsVisible = false;

        return;
    }

    private async Task InstallSIT(string version)
    {
        if (File.Exists(config.InstallPath + @"\EscapeFromTarkov_BE.exe"))
        {
            await CleanUpEFTDirectory();
        }

        switch (version)
        {
            case "3.6.1":
                {
                    if (config.TarkovVersion != "0.13.1.25206")
                    {
                        await MessageBoxManager.
                            GetMessageBoxStandard("Error", $"Your Tarkov version is incorrect for the selected SIT version.\nInstalled: {config.TarkovVersion}\nRequired: 0.13.1.25206", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).
                            ShowWindowDialogAsync(this);
                        return;
                    }

                    if (!Directory.Exists(config.InstallPath + @"\SITLauncher\CoreFiles"))
                        Directory.CreateDirectory(config.InstallPath + @"\SITLauncher\CoreFiles");

                    if (!Directory.Exists(config.InstallPath + @"\SITLauncher\Backup\CoreFiles"))
                        Directory.CreateDirectory(config.InstallPath + @"\SITLauncher\Backup\CoreFiles");

                    if (!Directory.Exists(config.InstallPath + @"\BepInEx\plugins"))
                    {
                        await DownloadFile("https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", config.InstallPath + @"\SITLauncher\BepInEx5.zip", "BepInEx5.zip");
                        System.IO.Compression.ZipFile.ExtractToDirectory(config.InstallPath + @"\SITLauncher\BepInEx5.zip", config.InstallPath, true);
                        Directory.CreateDirectory(config.InstallPath + @"\BepInEx\plugins");
                    }
                        

                    await DownloadFile("https://github.com/paulov-t/SIT.Core/releases/download/1.7.8611.22925/Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\CoreFiles\Assembly-CSharp.dll", "Assembly-CSharp.dll");
                    if (File.Exists(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"))
                        File.Copy(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\Backup\CoreFiles\Assembly-CSharp.dll", true);
                    File.Copy(config.InstallPath + @"\SITLauncher\CoreFiles\Assembly-CSharp.dll", config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", true);

                    await DownloadFile("https://github.com/paulov-t/SIT.Core/releases/download/1.7.8611.22925/SIT.Core.dll", config.InstallPath + @"\SITLauncher\CoreFiles\SIT.Core.dll", "SIT.Core.dll");                    
                    File.Copy(config.InstallPath + @"\SITLauncher\CoreFiles\SIT.Core.dll", config.InstallPath + @"\BepInEx\plugins\SIT.Core.dll", true);

                    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT_Unofficial_Launcher.Resources.Aki.Common.dll"))
                    {
                        using (var file = new FileStream(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Common.dll", FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }

                    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT_Unofficial_Launcher.Resources.Aki.Reflection.dll"))
                    {
                        using (var file = new FileStream(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Reflection.dll", FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }

                    await MessageBoxManager.GetMessageBoxStandard("Info", "Installation complete.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
                }
                break;
            case "3.7.0":
                {
                    if (config.TarkovVersion != "0.13.5.26282")
                    {
                        await MessageBoxManager.
                            GetMessageBoxStandard("Error", $"Your Tarkov version is incorrect for the selected SIT version.\nInstalled: {config.TarkovVersion}\nRequired: 0.13.5.26282", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).
                            ShowWindowDialogAsync(this);
                        return;
                    }

                    if (!Directory.Exists(config.InstallPath + @"\SITLauncher\CoreFiles"))
                        Directory.CreateDirectory(config.InstallPath + @"\SITLauncher\CoreFiles");

                    if (!Directory.Exists(config.InstallPath + @"\SITLauncher\Backup\CoreFiles"))
                        Directory.CreateDirectory(config.InstallPath + @"\SITLauncher\Backup\CoreFiles");

                    if (!Directory.Exists(config.InstallPath + @"\BepInEx\plugins"))
                    {
                        await DownloadFile("https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", config.InstallPath + @"\SITLauncher\BepInEx5.zip", "BepInEx5.zip");
                        System.IO.Compression.ZipFile.ExtractToDirectory(config.InstallPath + @"\SITLauncher\BepInEx5.zip", config.InstallPath, true);
                        Directory.CreateDirectory(config.InstallPath + @"\BepInEx\plugins");
                    }


                    await DownloadFile("https://github.com/paulov-t/SIT.Core/releases/download/1.9.8663.32326/Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\CoreFiles\Assembly-CSharp.dll", "Assembly-CSharp.dll");
                    if (File.Exists(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"))
                        File.Copy(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\Backup\CoreFiles\Assembly-CSharp.dll", true);
                    File.Copy(config.InstallPath + @"\SITLauncher\CoreFiles\Assembly-CSharp.dll", config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", true);

                    await DownloadFile("https://github.com/paulov-t/SIT.Core/releases/download/1.9.8663.32326/SIT.Core.dll", config.InstallPath + @"\SITLauncher\CoreFiles\SIT.Core.dll", "SIT.Core.dll");
                    File.Copy(config.InstallPath + @"\SITLauncher\CoreFiles\SIT.Core.dll", config.InstallPath + @"\BepInEx\plugins\SIT.Core.dll", true);

                    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT_Unofficial_Launcher.Resources.Aki.Common.dll"))
                    {
                        using (var file = new FileStream(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Common.dll", FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }

                    using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SIT_Unofficial_Launcher.Resources.Aki.Reflection.dll"))
                    {
                        using (var file = new FileStream(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Reflection.dll", FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }

                    await MessageBoxManager.GetMessageBoxStandard("Info", "Installation complete.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
                }
                break;
        }
    }

    private async Task DownloadFile(string url, string filePath, string fileName)
    {
        StatusText.Text = "Downloading file: " + fileName;
        StatusText.IsVisible = true;
        StatusProgressBar.IsVisible = true;

        byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(filePath, fileBytes);

        StatusText.Text = "Downloading file: " + fileName;
        StatusText.IsVisible = false;
        StatusProgressBar.IsVisible = false;
    }

    private async Task<string> LoginToServer()
    {
        if (string.IsNullOrEmpty(AddressBox.Text))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "No server address provided", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }

        if (AddressBox.Text.EndsWith("/"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Server address is incorrect, you should NOT have a / at the end", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }

        if (string.IsNullOrEmpty(UsernameBox.Text))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "You cannot use an empty username for your account", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }

        if (string.IsNullOrEmpty(PasswordBox.Text))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "You cannot use an empty password for your account", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }

        TarkovRequesting requesting = new TarkovRequesting(null, AddressBox.Text, false);

        Dictionary<string, string> data = new Dictionary<string, string>
        {
            { "username", UsernameBox.Text },
            { "email", UsernameBox.Text },
            { "edition", "Edge Of Darkness" },
            { "password", PasswordBox.Text }
        };

        try
        {
            var returnData = requesting.PostJson("/launcher/profile/login", JsonSerializer.Serialize(data));

            // If failed, attempt to register
            if (returnData == "FAILED")
            {
                var msgBoxResult = await MessageBoxManager.GetMessageBoxStandard("Account Not Found", "Your account has not been found, would you like to register a new account with these credentials?", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                if (msgBoxResult == ButtonResult.Yes)
                {
                    // Register
                    returnData = requesting.PostJson("/launcher/profile/register", JsonSerializer.Serialize(data));
                    // Login attempt after register
                    returnData = requesting.PostJson("/launcher/profile/login", JsonSerializer.Serialize(data));

                }
                else
                {
                    return null;
                }
            }

            return returnData;
        }
        catch (System.Net.WebException webEx)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Unable to communicate with the Server\n{webEx.Message}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Unable to communicate with the Server\n{ex.Message}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return "error";
        }
        return null;
    }

    private async void OnConnectClick(object sender, RoutedEventArgs e)
    {
        config.Save(config);

        if (!File.Exists(config.InstallPath + @"\BepInEx\plugins\SIT.Core.dll"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Unable to find 'SIT.Core' in installation folder. Make sure SIT is installed.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        string returnData = await Dispatcher.UIThread.InvokeAsync(LoginToServer);

        if (returnData == "error")
            return;

        if (string.IsNullOrEmpty(returnData))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Something went wrong when retrieving data from the server or the action was cancelled. Check the logs.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        if (!File.Exists(config.InstallPath + @"\BepInEx\plugins\SIT.Core.dll"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Unable to find 'SIT.Core.dll' in install path. Make sure SIT is installed.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        if (!File.Exists(config.InstallPath + @"\EscapeFromTarkov.exe"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Unable to find 'EscapeFromTarkov.exe' in install path.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
        Process.Start(config.InstallPath + @"\EscapeFromTarkov.exe", arguments);
        config.Save(config);
        WindowState = WindowState.Minimized;

        if (config.CloseAfterLaunch)
        {
            Close();
        }

    }

    private async void OnChangeInstallPathClick(object sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
            Title = "Select EFT Folder",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync((config.InstallPath == null) ? Directory.GetCurrentDirectory() : config.InstallPath) 
        });
        if (result.FirstOrDefault() != null)
        {
            if (!File.Exists(result.FirstOrDefault().TryGetLocalPath() + @"\EscapeFromTarkov.exe"))
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandard("Error", $"The select folder does not contain 'EscapeFromTarkov.exe'. Please select a valid folder.\nPath: {result.FirstOrDefault().TryGetLocalPath()}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                await msgBox.ShowWindowDialogAsync(this);
                return;
            }

            config.InstallPath = result.FirstOrDefault().TryGetLocalPath();
            string fileVersion = FileVersionInfo.GetVersionInfo(result.FirstOrDefault().TryGetLocalPath() + @"\EscapeFromTarkov.exe").FileVersion;
            config.TarkovVersion = fileVersion;
            config.Save(config);

            await MessageBoxManager.GetMessageBoxStandard("Info", $"Your selected EFT version is: {fileVersion}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }        

    }

    private async void OnCheckVersionClick(object sender, RoutedEventArgs e)
    {
        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "There is no install path selected.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        string path = config.InstallPath + @"\EscapeFromTarkov.exe";
        if (File.Exists(path))
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
            config.TarkovVersion = fileVersion;
            await MessageBoxManager.GetMessageBoxStandard("Info", $"Your selected EFT version is: {fileVersion}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Could not locate 'EscapeFromTarkov.exe'.\nPath: {path}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }
    }

    //private async void OnCleanupDirClick(object sender, RoutedEventArgs e)
    //{
    //    await CleanUpEFTDirectory();
    //}

    private async void OnInstallSITClick(object sender, RoutedEventArgs e)
    {
        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "There is no install path selected.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }
        var msgBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = "Select Version",
            ContentMessage = "Select the version of SIT to install.\n",
            Icon = MsBox.Avalonia.Enums.Icon.Setting,
            ButtonDefinitions = new List<ButtonDefinition>()
            {
                new() { Name = "3.6.1" },
                new() { Name = "3.7.0" }
            }
        });
        var result = await msgBox.ShowWindowDialogAsync(this);
        if (result != null)
        {
            await InstallSIT(result);
        }
    }

    private async void OnOpenPluginsFolderClick(object sender, RoutedEventArgs e)
    {
        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Install Path is not configured.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        string dirPath = config.InstallPath + @"\BepInEx\plugins\";
        if (Directory.Exists(dirPath))
            Process.Start("explorer.exe", dirPath);
        else
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Could not find the given path. Is BepInEx installed?\nPath: {dirPath}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }
    }

    private async void OnSITConfigClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(config.InstallPath + @"\BepInEx\config\SIT.Core.cfg"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Could not find 'SIT.Core.cfg'. Make sure SIT is installed and that you have started the game once.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        Process.Start("explorer.exe", config.InstallPath + @"\BepInEx\config\SIT.Core.cfg");
    }

    private async void OnOpenEFTFolderClick(object sender, RoutedEventArgs e)
    {
        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Install Path is not configured.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }
        if (Directory.Exists(config.InstallPath))
            Process.Start("explorer.exe", config.InstallPath);
    }

    private async void OnDownloadPatcherClick(object sender, RoutedEventArgs e)
    {
        await MessageBoxManager.GetMessageBoxStandard("Information", $"You will now be redirected to the download page for the 'AKI DowngradePatcher'.\nAfter downloading the file, extract the contents to your 'Install Path' folder and run the 'Patcher.exe'.\nAfter the Patcher is done it's recommended to reselect your 'Install Path' under 'Settings' to update the installed version.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        Process.Start("explorer.exe", "https://hub.sp-tarkov.com/files/file/204-aki-patcher/#versions");
    }

    //private async void OnAddServerClick(object sender, RoutedEventArgs e)
    //{
    //    MessageBoxCustomParams customParams = new()
    //    {
    //        ContentMessage = "Select the name of the server:",
    //        ContentTitle = "Add New Server",
    //        CanResize = false,
    //        Width = 400,
    //        Icon = MsBox.Avalonia.Enums.Icon.Plus,
    //        InputParams = new()
    //        {
    //            Label = "Name:",
    //            DefaultValue = ""
    //        },
    //        ButtonDefinitions = new List<ButtonDefinition>()
    //        {
    //            new ButtonDefinition() { Name = "Add" },
    //            new ButtonDefinition() { Name = "Cancel", IsCancel = true }
    //        }
    //    };
    //    var box = MessageBoxManager
    //        .GetMessageBoxCustom(customParams);

    //    var result = await box.ShowWindowDialogAsync(this);
    //    if (result == "Add")
    //    {
    //        config.Servers.Add(new Server { Name = box.InputValue, Address = AddressBox.Text });
    //        config.Save(config);
    //    }        
    //}

    //private void OnRemoveServerClick(object sender, RoutedEventArgs e)
    //{
    //    config.Servers.RemoveAt(ServerCombo.SelectedIndex);
    //    config.Save(config);

    //    if (config.Servers.Count > 0)
    //        ServerCombo.SelectedIndex = 0;
    //}
}
