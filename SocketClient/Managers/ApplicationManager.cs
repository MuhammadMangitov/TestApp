using SocketClient.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ApplicationMonitor;
using DBHelper;
using DgzAIO.HttpService;

namespace SocketClient.Managers
{
    public class ApplicationManager : Interfaces.IApplicationManager
    {
        private readonly Interfaces.IFileDownloader _fileDownloader;
        private readonly Interfaces.IRegistryHelper _registryHelper;
        private readonly Interfaces.IConfiguration _config;
        private readonly Interfaces.ILogger _logger;

        public ApplicationManager(Interfaces.IFileDownloader fileDownloader, Interfaces.IRegistryHelper registryHelper, Interfaces.IConfiguration config, Interfaces.ILogger logger)
        {
            _fileDownloader = fileDownloader;
            _registryHelper = registryHelper;
            _config = config;
            _logger = logger;
        }

        public async Task<bool> InstallApplicationAsync(string appName, string command, string[] arguments)
        {
            try
            {
                string jwtToken = await SQLiteHelper.GetJwtToken();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    _logger.LogError("JWT token topilmadi!");
                    return false;
                }

                string requestUrl = $"{_config.GetApiUrl()}{appName}";
                _logger.LogInformation($"Install app URL: {requestUrl}");

                string installerFolder = Path.Combine("C:\\Program Files (x86)", "DgzAIO", "Installers");
                Directory.CreateDirectory(installerFolder);

                string savePath = Path.Combine(installerFolder, appName);
                _logger.LogInformation($"Installer file path: {savePath}");

                bool downloaded = await _fileDownloader.DownloadFileAsync(requestUrl, savePath, jwtToken);
                if (!downloaded)
                {
                    _logger.LogError("Fayl yuklab olishda xatolik yuz berdi.");
                    return false;
                }

                if (!File.Exists(savePath))
                {
                    _logger.LogError($"Fayl topilmadi: {savePath}");
                    return false;
                }

                bool isMsi = Path.GetExtension(savePath).Equals(".msi", StringComparison.OrdinalIgnoreCase);
                bool installationSucceeded = false;

                foreach (var arg in arguments)
                {
                    _logger.LogInformation($"Trying install with argument: {arg}");

                    bool result = await TryInstallAsync(savePath, arg, isMsi);
                    if (result)
                    {
                        SendApplicationForSocketAsync().Wait();
                        _logger.LogInformation($"Installation succeeded with argument: {arg}");

                        installationSucceeded = true;
                        break; // O'rnatish muvaffaqiyatli bo'lsa, qolgan argumentlar bilan sinab ko'rishni to'xtatish
                    }
                    else
                    {
                        _logger.LogInformation($"Installation failed with argument: {arg}");
                    }
                }

                if (installationSucceeded)
                {
                    await Task.Delay(3000); // O'rnatishdan so'ng 3 soniya kutish

                    try
                    {
                        if (File.Exists(savePath))
                        {
                            File.Delete(savePath);
                            _logger.LogInformation($"Installer fayli o'chirildi: {savePath}");
                        }
                        else
                        {
                            _logger.LogError($"Installer fayli topilmadi o'chirish uchun: {savePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Delete error: {ex}");
                    }

                    await SendApplicationForSocketAsync();
                    return true;
                }
                else
                {
                    _logger.LogError("All installation attempts failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Installation error: {ex}");
                return false;
            }
        }
        private async Task<bool> TryInstallAsync(string filePath, string arguments, bool isMsi)
        {
            try
            {
                _logger.LogInformation($"Starting process: {filePath} with arguments: {arguments} and isMsi: {isMsi}");
                using (var process = new Process())
                {
                    if (isMsi)
                    {
                        process.StartInfo.FileName = "msiexec";
                        process.StartInfo.Arguments = $"/i \"{filePath}\" {arguments}";
                        _logger.LogInformation($"msiexec command: {process.StartInfo.Arguments}");
                    }
                    else
                    {
                        process.StartInfo.FileName = filePath;
                        process.StartInfo.Arguments = $"{arguments}";
                        _logger.LogInformation($"Executable command: {process.StartInfo.Arguments}");
                    }

                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(filePath);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                    process.Start();
                    _logger.LogInformation("Process started.");

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit(30000));

                    if (!process.HasExited)
                    {
                        process.Kill();
                        _logger.LogInformation("Process timed out and was killed.");
                        return false;
                    }

                    _logger.LogInformation($"Exit Code: {process.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(output)) _logger.LogInformation($"Output: {output}");
                    if (!string.IsNullOrWhiteSpace(error)) _logger.LogError($"Error Output: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TryInstallAsync error: {ex}");
                return false;
            }
        }
        public async Task<bool> UninstallApplicationAsync(string appName, string[] arguments)
        {
            try
            {
                // Registry'dan uninstall string olish
                string uninstallString = _registryHelper.GetUninstallString(appName);
                _logger.LogInformation($"Uninstall string: {uninstallString} for {appName}");
                if (string.IsNullOrEmpty(uninstallString))
                {
                    _logger.LogError($"Uninstall string for {appName} not found.");
                    return false;
                }

                // Har bir argumentni ishlash
                foreach (var argument in arguments)
                {
                    string fullUninstallCommand = $"\"{uninstallString}\" {argument}";

                    _logger.LogInformation($"Uninstalling {appName} with argument: {argument}");
                    int exitCode = await ExecuteProcessAsync("cmd.exe", $"/C {fullUninstallCommand}");
                    if(exitCode == 0)
                    {
                        SendApplicationForSocketAsync().Wait();
                    }   
                    if (exitCode != 0)
                    {
                        _logger.LogError($"Error uninstalling {appName}, exit code: {exitCode}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during uninstallation of {appName}: {ex.Message}");
                return false;
            }
        }
        private async Task<int> ExecuteProcessAsync(string fileName, string arguments)
        {
            try
            {
                _logger.LogInformation($"Executing process: {fileName} with arguments: {arguments}");
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();

                    // Output va error oqimlarini o‘qish
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // 20 sekund kutish va kill qilish logikasi
                    var delayTask = Task.Delay(20000);
                    var waitForExitTask = Task.Run(() => process.WaitForExit());

                    var completedTask = await Task.WhenAny(waitForExitTask, delayTask);

                    if (completedTask == delayTask && !process.HasExited)
                    {
                        process.Kill(); // Bu faqat asosiy processni o‘ldiradi
                        _logger.LogInformation("Process timed out and was killed.");
                    }

                    // Output va errorni yakunlash
                    string output = await outputTask;
                    string error = await errorTask;

                    if (!string.IsNullOrWhiteSpace(output))
                        _logger.LogInformation($"Process Output: {output}");
                    if (!string.IsNullOrWhiteSpace(error))
                        _logger.LogError($"Process Error: {error}");

                    _logger.LogInformation($"Process Exit Code: {process.ExitCode}");
                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing process: {ex.Message}");
                return -1;
            }
        }

        public bool CloseApplication(string appName)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(appName))
                {
                    process.Kill();
                    process.WaitForExit();
                }
                _logger.LogInformation($"Application {appName} closed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error closing application {appName}: {ex.Message}");
                return false;
            }
        }
        public async Task SendApplicationForSocketAsync()
        {
            _logger.LogInformation("[Application Monitor] Retrieving installed programs...");

            var programs = await ApplicationMonitor.ApplicationMonitor.GetInstalledPrograms();
            bool success = await ApiClient.SendProgramInfo(programs);

            if (success)
            {
                _logger.LogInformation("Installed programs list sent to server.");
            }
            else
            {
                _logger.LogError("Error sending installed programs list to server.");
            }
        }
    }
}