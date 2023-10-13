using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;
using SIT_Unofficial_Launcher.Classes;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Archives;
using SIT_Unofficial_Launcher.Utils;

namespace SIT_Unofficial_Launcher.Views;

public partial class MainWindow : Window
{
    LauncherConfig config = new();
    HttpClient httpClient = new() { 
        Timeout = Timeout.InfiniteTimeSpan, 
        DefaultRequestHeaders = { 
            { "X-GitHub-Api-Version", "2022-11-28" }, 
            { "User-Agent", "request" } 
        } 
    };

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

    private async Task<bool> DownloadFile(string fileName, string filePath, string fileUrl, bool showProgress = false)
    {
        try
        {
            if (showProgress == true)
                Dispatcher.UIThread.Post(() => ShowProgress(true, $"Downloading '{fileName}'"));

            filePath = filePath + $@"\{fileName}";

            var progress = new Progress<float>((prog) => { Dispatcher.UIThread.Post(() => { StatusProgressBar.Value = (int)Math.Floor(prog); }); });
            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                await HttpClientProgressExtensions.DownloadDataAsync(httpClient, fileUrl, file, progress);

            if (showProgress == true)
                Dispatcher.UIThread.Post(() => ShowProgress(false));

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private void ShowProgress(bool enabled, string text = null)
    {
        if (!enabled)
        {
            StatusProgressBar.IsVisible = false;
            StatusText.Text = null;
            return;
        }

        if (StatusProgressBar.IsVisible == false)
            StatusProgressBar.IsVisible = true;

        if (text != null)
        {
            StatusText.Text = text;

            if (StatusText.IsVisible != true)
                StatusText.IsVisible = true;
        }
        else
        {
            StatusText.Text = null;

            if (StatusText.IsVisible == true)
                StatusText.IsVisible = false;
        }
    }    

    private async Task<string> RunPatcher()
    {
        if (!File.Exists(config.InstallPath + @"\Patcher.exe"))
            return null;

        Process patcherProcess = new()
        {
            StartInfo = new()
            {
                FileName = config.InstallPath + @"\Patcher.exe",
                WorkingDirectory = config.InstallPath,
                Arguments = "autoclose"
            },
            EnableRaisingEvents = true
        };
        patcherProcess.Start();
        await patcherProcess.WaitForExitAsync();

        string patcherResult = null;

        switch (patcherProcess.ExitCode)
        {
            case 0:
                {
                    patcherResult = "Patcher was closed.";
                    break;
                }
            case 10:
                {
                    patcherResult = "Patcher was succesful.";
                    if (File.Exists(config.InstallPath + @"\Patcher.exe"))
                        File.Delete(config.InstallPath + @"\Patcher.exe");

                    if (File.Exists(config.InstallPath + @"\Patcher.log"))
                        File.Delete(config.InstallPath + @"\Patcher.log");

                    if (Directory.Exists(config.InstallPath + @"\Aki_Patches"))
                        Directory.Delete(config.InstallPath + @"\Aki_Patches", true);

                    break;
                }
            case 11:
                {
                    patcherResult = "Could not find 'EscapeFromTarkov.exe'.";
                    break;
                }
            case 12:
                {
                    patcherResult = "'Aki_Patches' is missing.";
                    break;
                }
            case 13:
                {
                    patcherResult = "Install folder is missing a file.";
                    break;
                }
            case 14:
                {
                    patcherResult = "Install folder is missing a folder.";
                    break;
                }
            case 15:
                {
                    patcherResult = "Patcher failed.";
                    break;
                }
            default:
                {
                    patcherResult = "Unknown error.";
                    break;
                }

        }
        return patcherResult;
    }

    private async Task DownloadPatcher()
    {
        GiteaRelease selectedVersion = null;

        string releasesString = await httpClient.GetStringAsync(@"https://dev.sp-tarkov.com/api/v1/repos/SPT-AKI/Downgrade-Patches/releases");
        List<GiteaRelease> giteaReleases = JsonSerializer.Deserialize<List<GiteaRelease>>(releasesString);

        string tarkovVersion = config.TarkovVersion.Split(".").Last();

        selectedVersion = await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SelectPatcherVersion selectWindow = new(giteaReleases, config.TarkovVersion) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
            selectedVersion = await selectWindow.ShowDialog<GiteaRelease>(this);
            return selectedVersion;
        });

        if (selectedVersion == null)
            return;

        if (selectedVersion.name.Split(" to ")[0] != config.TarkovVersion.Split(".").Last())
        {
            bool answer = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ButtonResult warningResult = await MessageBoxManager.
                GetMessageBoxStandard("Error", $"Your Tarkov version is incorrect for the selected patcher.\nAre you sure you want to continue?\n\nInstalled: {config.TarkovVersion.Split(".").Last()}\nRequired: {selectedVersion.name.Split(" to ")[0]}", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Error).
                ShowWindowDialogAsync(this);
                if (warningResult == ButtonResult.No)
                    return false;
                else
                    return true;
            });
            if (answer == false)
                return;
        }

        string mirrorsUrl = selectedVersion.assets.Find(q => q.name == "mirrors.json").browser_download_url;
        string mirrorsString = await httpClient.GetStringAsync(mirrorsUrl);
        List<Mirrors> mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsString);
        string link = null;

        foreach (Mirrors mirror in mirrors)
        {
            if (mirror.Link.Contains("gofile.io"))
            {
                link = mirror.Link;
                break;
            }
        }

        if (link == null)
            return;

        bool success = await DownloadFile("patcher.zip", config.InstallPath, link, true);

        if (success == false)
            return;

        using ZipArchive zip = ZipArchive.Open(config.InstallPath + @"\patcher.zip");
        var files = zip.Entries;

        foreach (var file in files)
        {
            if (file.IsDirectory == false)
            {
                file.WriteToDirectory(config.InstallPath, new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }            
        }

        var patcherDir = Directory.GetDirectories(config.InstallPath, "Patcher*").First();

        await UtilFunctions.CloneDirectory(patcherDir, config.InstallPath);
        Directory.Delete(patcherDir, true);

        string result = await RunPatcher();

        if (result != "Patcher was succesful." || result == null)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", result, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            });
        }
        else
        {
            Dispatcher.UIThread.Post(async () =>
            {
                await MessageBoxManager.GetMessageBoxStandard("Success", result, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success).ShowAsync();
            });
        }

    }

    private async Task InstallSIT()
    {
        GithubRelease selectedVersion = null;        

        try
        {

            string releasesString = await httpClient.GetStringAsync(@"https://api.github.com/repos/paulov-t/SIT.Core/releases");
            List<GithubRelease> githubReleases = new();
            githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesString);

            if (githubReleases.Count > 0)
            {
                foreach (GithubRelease release in githubReleases)
                {
                    var match = Regex.Match(release.body, @"[0]{1,}\.[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,5}");
                    if (match.Success)
                    {
                        release.tag_name = release.name + " - Tarkov Version: " + match.Value;
                        release.body = match.Value;
                    }
                }

                SelectSitVersion selectWindow = new(githubReleases, config.TarkovVersion) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
                selectedVersion = await selectWindow.ShowDialog<GithubRelease>(this);

                if (selectedVersion == null)
                    return;
            }
            else
                return;

        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(ex.Message);
        }

        if (File.Exists(config.InstallPath + @"\EscapeFromTarkov_BE.exe"))
        {
            await CleanUpEFTDirectory();
        }

        if (config.TarkovVersion != selectedVersion.body)
        {
            ButtonResult result = await MessageBoxManager.
                GetMessageBoxStandard("Error", $"Your Tarkov version is incorrect for the selected SIT version.\nAre you sure you want to continue?\n\nInstalled: {config.TarkovVersion}\nRequired: {selectedVersion.body}", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Error).
                ShowWindowDialogAsync(this);
            if (result == ButtonResult.No)
            {
                return;
            }            
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

        //We don't use index as they might be different from version to version
        string assemblyUrl = selectedVersion.assets.Find(q => q.name == "Assembly-CSharp.dll").browser_download_url;
        string sitcoreUrl = selectedVersion.assets.Find(q => q.name == "SIT.Core.dll").browser_download_url;

        await DownloadFile("Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\CoreFiles", assemblyUrl, true);

        if (File.Exists(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"))
            File.Copy(config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", config.InstallPath + @"\SITLauncher\Backup\CoreFiles\Assembly-CSharp.dll", true);
        File.Copy(config.InstallPath + @"\SITLauncher\CoreFiles\Assembly-CSharp.dll", config.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", true);

        await DownloadFile("SIT.Core.dll", config.InstallPath + @"\SITLauncher\CoreFiles", sitcoreUrl, true);

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

    private async Task SelectInstallPath()
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
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
            string fileVersion = FileVersionInfo.GetVersionInfo(result.FirstOrDefault().TryGetLocalPath() + @"\EscapeFromTarkov.exe").ProductVersion;
            fileVersion = Regex.Match(fileVersion, @"[0]{1,}\.[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,2}\-[0-9]{1,5}").Value.Replace("-", ".");
            config.TarkovVersion = fileVersion;

            if (config.RememberLogin == true)
                config.Save(config, true);
            else
                config.Save(config);

            await MessageBoxManager.GetMessageBoxStandard("Info", $"Your selected EFT version is: {fileVersion}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }
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
    }

    #region BUTTONS

    private async void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (config.RememberLogin == true)
            config.Save(config, true);
        else
            config.Save(config);

        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Install Path has not been selected. Go to 'Settings' and select it.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        if (!File.Exists(config.InstallPath + @"\BepInEx\plugins\SIT.Core.dll"))
        {
            ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Error", "Unable to find 'SIT.Core' in the installation folder. Would you like to install it now?", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            if (result == ButtonResult.Yes)
            {
                InstallSIT();
                return;
            }
            else
                return;
        }

        if (!File.Exists(config.InstallPath + @"\EscapeFromTarkov.exe"))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "Unable to find 'EscapeFromTarkov.exe' in install path.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
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

        string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
        Process.Start(config.InstallPath + @"\EscapeFromTarkov.exe", arguments);

        WindowState = WindowState.Minimized;

        if (config.CloseAfterLaunch)
        {
            Close();
        }

    }

    private async void OnDownloadPatcherClick(object sender, RoutedEventArgs e)
    {
        //await DownloadPatcher();
        // Run this as a Task to prevent UI freezes during IO Operations
        await Task.Run(DownloadPatcher);
    }

    private async void OnChangeInstallPathClick(object sender, RoutedEventArgs e)
    {
        await SelectInstallPath();
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
            string fileVersion = FileVersionInfo.GetVersionInfo(path).ProductVersion;
            fileVersion = Regex.Match(fileVersion, @"[0]{1,}\.[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,2}\-[0-9]{1,5}").Value.Replace("-", ".");
            config.TarkovVersion = fileVersion;
            await MessageBoxManager.GetMessageBoxStandard("Info", $"Your selected EFT version is: {fileVersion}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", $"Could not locate 'EscapeFromTarkov.exe'.\nPath: {path}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowWindowDialogAsync(this);
        }
    }

    private async void OnInstallSITClick(object sender, RoutedEventArgs e)
    {
        if (config.InstallPath == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "There is no install path selected.", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync(this);
            return;
        }

        await InstallSIT();        
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

    #endregion
}