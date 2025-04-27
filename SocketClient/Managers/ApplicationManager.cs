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

        public async Task<bool> InstallApplicationAsync(string appName, string command)
        {
            try
            {
                string jwtToken = await SQLiteHelper.GetJwtToken();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    _logger.LogError("JWT token topilmadi!");
                    return false;
                }

                _logger.LogInformation($"Install app token: {jwtToken}");

                string requestUrl = $"{_config.GetApiUrl()}{appName}";
                _logger.LogInformation($"Install app URL: {requestUrl}");

                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string installerFolder = Path.Combine(programData, "DgzAIO", "Installers");
                Directory.CreateDirectory(installerFolder); // papkani yaratib olamiz, agar mavjud bo'lmasa
                string savePath = Path.Combine(installerFolder, appName);


                _logger.LogInformation($"Installer file path: {savePath}");
                Console.WriteLine($"Installer file path: {savePath}");
                // Faylni yuklab olish
                bool downloaded = await _fileDownloader.DownloadFileAsync(requestUrl, savePath, jwtToken);
                if (!downloaded)
                {
                    _logger.LogError("Fayl yuklab olishda xatolik yuz berdi.");
                    return false;
                }

                // Fayl mavjudligini tekshirish
                if (!File.Exists(savePath))
                {
                    _logger.LogError($"Fayl topilmadi: {savePath}");
                    return false;
                }

                _logger.LogInformation($"Installer file saved at: {savePath}");

                // Sinab ko'riladigan silent parametrlar
                string[] silentCommands = new[]
                 {
                    "/S",
                    "/silent",
                    "/verysilent",
                    "/quiet",
                    "/qn",
                    "/s /v\"/qn /norestart\""
                };


                bool installationSucceeded = false;

                foreach (var silentCommand in silentCommands)
                {
                    _logger.LogInformation($"Trying to install with command: {silentCommand}");

                    installationSucceeded = await TryInstallAsync(savePath, silentCommand);
                    if (installationSucceeded)
                    {
                        _logger.LogInformation($"Successfully installed with command: {silentCommand}");
                        break;
                    }
                    else
                    {
                        _logger.LogError($"Installation failed with command: {silentCommand}");
                    }
                }

                if (installationSucceeded)
                {
                    await SendApplicationForSocketAsync();

                    // Installer faylini o'chirib tashlash
                    string deleteCommand = $"/C timeout /t 3 & del \"{savePath}\"";
                    Process.Start(new ProcessStartInfo("cmd.exe", deleteCommand)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });

                    return true;
                }
                else
                {
                    _logger.LogError("All silent install attempts failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during installation: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TryInstallAsync(string filePath, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = filePath;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(filePath);
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.Verb = "runas";

                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit());

                    _logger.LogInformation($"Exit Code: {process.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(output)) _logger.LogInformation($"Output: {output}");
                    if (!string.IsNullOrWhiteSpace(error)) _logger.LogError($"Error: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TryInstallAsync error: {ex.Message}");
                return false;
            }
        }


        /* public async Task<bool> InstallApplicationAsync(string appName, string command)
        {
            try
            {
                string jwtToken = await SQLiteHelper.GetJwtToken();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    _logger.LogError("JWT token topilmadi!");
                    return false;
                }
                _logger.LogInformation($"Install app token: {jwtToken}");
                string requestUrl = $"{_config.GetApiUrl()}{appName}";
                _logger.LogInformation($"Install app URL: {requestUrl}");
                string savePath = Path.Combine(Path.GetTempPath(), appName);

                bool downloaded = await _fileDownloader.DownloadFileAsync(requestUrl, savePath, jwtToken);
                if (downloaded && (command != "update_app" || CloseApplication(appName)))
                {
                    _logger.LogInformation($"Install command: {command}");
                    bool success = await RunProcessAsync(savePath, "/silent /verysilent /norestart");
                    _logger.LogInformation($"Install app success: {success}");
                    if (success)
                    {
                        await SendApplicationForSocketAsync();
                        string deleteCommand = $"/C timeout /t 3 & del \"{savePath}\"";
                        Process.Start(new ProcessStartInfo("cmd.exe", deleteCommand) { CreateNoWindow = true, UseShellExecute = false });
                    }
                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{command.ToUpper()} error: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> RunProcessAsync1(string filePath, string arguments)
        {
            try
            {
                _logger.LogInformation($"Starting process: {filePath} with arguments: {arguments}");
                using (var process = new Process())
                {
                    process.StartInfo.FileName = filePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Verb = "runas"; // Run as administrator
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    _logger.LogInformation("Process started.");

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit());
                    _logger.LogInformation($"Process exited with code: {process.ExitCode}");

                    if (!string.IsNullOrEmpty(output)) _logger.LogInformation($"Process output: {output}");
                    if (!string.IsNullOrEmpty(error)) _logger.LogError($"Process error: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RunProcessAsync error: {ex.Message}");
                return false;
            }
        }*/

        /* public async Task<bool> InstallApplicationAsync(string appName, string command)
         {
             try
             {
                 string jwtToken = await SQLiteHelper.GetJwtToken();
                 if (string.IsNullOrEmpty(jwtToken))
                 {
                     _logger.LogError("JWT token topilmadi!");
                     return false;
                 }

                 _logger.LogInformation($"Install app token: {jwtToken}");

                 string requestUrl = $"{_config.GetApiUrl()}{appName}";
                 _logger.LogInformation($"Install app URL: {requestUrl}");
                 string savePath = $"C:\\Users\\Muhammad\\Desktop\\" + $"{appName}";

                 // Yuklab olish
                 bool downloaded = await _fileDownloader.DownloadFileAsync(requestUrl, savePath, jwtToken);
                 if (!downloaded)
                 {
                     _logger.LogError("Fayl yuklab olishda xatolik yuz berdi.");
                     return false;
                 }

                 _logger.LogInformation($"Install command: {command}");

                 // Fayl mavjudligini tekshirish
                 if (!File.Exists(savePath))
                 {
                     _logger.LogError($"Fayl topilmadi: {savePath}");
                     return false;
                 }

                 // Admin huquqlari bilan jarayonni ishga tushirish
                 using (var process = new Process())
                 {
                     process.StartInfo.FileName = savePath; // Fayl nomini qavs ichida
                     process.StartInfo.Arguments = "/silent /verysilent /norestart";
                     process.StartInfo.UseShellExecute = false;
                     process.StartInfo.Verb = "runas"; // Administrator huquqlarini so‘rash
                     process.StartInfo.CreateNoWindow = true;
                     process.StartInfo.RedirectStandardOutput = true;
                     process.StartInfo.RedirectStandardError = true;

                     process.Start();
                     _logger.LogInformation("Process started.");

                     // StandardOutput va StandardError'ni o'qish
                     string output = await process.StandardOutput.ReadToEndAsync();
                     string error = await process.StandardError.ReadToEndAsync();

                     // Jarayonni kutish
                     await Task.Run(() => process.WaitForExit()); // Asynchronous wait for process exit

                     _logger.LogInformation($"Process exited with code: {process.ExitCode}");

                     // Chiqish va xatoliklarni loglash
                     if (!string.IsNullOrEmpty(output)) _logger.LogInformation($"Process output: {output}");
                     if (!string.IsNullOrEmpty(error)) _logger.LogError($"Process error: {error}");

                     if (process.ExitCode == 0)
                     {
                         _logger.LogInformation("Install app success.");
                         await SendApplicationForSocketAsync();

                         string deleteCommand = $"/C timeout /t 3 & del \"{savePath}\"";
                         Process.Start(new ProcessStartInfo("cmd.exe", deleteCommand) { CreateNoWindow = true, UseShellExecute = false });

                         return true;
                     }
                     else
                     {
                         _logger.LogError($"Installation failed with exit code: {process.ExitCode}");
                         return false;
                     }
                 }
             }
             catch (Exception ex)
             {
                 _logger.LogError($"Error during installation: {ex.Message}");
                 return false;
             }
         }
 */


        /* public async Task<bool> InstallApplicationAsync(string appName, string command)
         {
             try
             {
                 string jwtToken = await SQLiteHelper.GetJwtToken();
                 if (string.IsNullOrEmpty(jwtToken))
                 {
                     _logger.LogError("JWT token topilmadi!");
                     return false;
                 }
                 _logger.LogInformation($"Install app token: {jwtToken}");
                 string requestUrl = $"{_config.GetApiUrl()}{appName}";
                 _logger.LogInformation($"Install app URL: {requestUrl}");
                 string savePath = Path.Combine(Path.GetTempPath(), appName);

                 bool downloaded = await _fileDownloader.DownloadFileAsync(requestUrl, savePath, jwtToken);
                 if (downloaded && (command != "update_app" || CloseApplication(appName)))
                 {
                     _logger.LogInformation($"Install command: {command}");
                     bool success = await RunProcessAsync(savePath, "/silent /verysilent /norestart");
                     _logger.LogInformation($"Install app success: {success}");
                     if (success)
                     {
                         await SendApplicationForSocketAsync();
                         string deleteCommand = $"/C timeout /t 3 & del \"{savePath}\"";
                         Process.Start(new ProcessStartInfo("cmd.exe", deleteCommand) { CreateNoWindow = true, UseShellExecute = false });
                     }
                     return success;
                 }
                 return false;
             }
             catch (Exception ex)
             {
                 _logger.LogError($"{command.ToUpper()} error: {ex.Message}");
                 return false;
             }
         }
 */


        /*public async Task<bool> UninstallApplicationAsync(string appName)
        {
            try
            {
                string uninstallString = _registryHelper.GetUninstallString(appName);
                if (string.IsNullOrEmpty(uninstallString))
                {
                    _logger.LogError($"Uninstall string not found for {appName}");
                    return false;
                }

                bool success = await RunProcessAsync("cmd.exe", $"/C \"{uninstallString} /silent /quiet /norestart\"");
                if (success)
                {
                    await SendApplicationForSocketAsync();
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred while uninstalling {appName} - {ex.Message}");
                return false;
            }
        }
        private async Task<bool> RunProcessAsync(string filePath, string arguments)
        {
            try
            {
                _logger.LogInformation($"Starting process: {filePath} with arguments: {arguments}");
                using (var process = new Process())
                {
                    process.StartInfo.FileName = filePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Verb = "runas"; // Run as administrator
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    _logger.LogInformation("Process started.");

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await Task.Run(() => process.WaitForExit());
                    _logger.LogInformation($"Process exited with code: {process.ExitCode}");

                    if (!string.IsNullOrEmpty(output)) _logger.LogInformation($"Process output: {output}");
                    if (!string.IsNullOrEmpty(error)) _logger.LogError($"Process error: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RunProcessAsync error: {ex.Message}");
                return false;
            }
        }*/

        public async Task<bool> UninstallApplicationAsync(string appName)
        {
            try
            {
                string uninstallString = _registryHelper.GetUninstallString(appName);
                _logger.LogInformation($"Uninstall string: {uninstallString}");
                if (string.IsNullOrEmpty(uninstallString))
                {
                    _logger.LogError($"Uninstall string not found for {appName}");
                    Console.WriteLine($"Uninstall string not found for {appName}");
                    return false;
                }

                // Silent uninstall parametrlarini sinash
                string[] silentUninstallCommands = new[] { "/S", "/silent", "/verysilent", "/quiet", "/qn", "/uninstall" };

                bool uninstallSucceeded = false;

                foreach (var silentCommand in silentUninstallCommands)
                {
                    Console.WriteLine($"Trying to uninstall with command: {silentCommand}");
                    _logger.LogInformation($"Trying to uninstall with command: {silentCommand}");

                    uninstallSucceeded = await RunProcessAsync("cmd.exe", $"/C \"{uninstallString}\" {silentCommand} /norestart");

                    if (uninstallSucceeded)
                    {
                        _logger.LogInformation($"Successfully uninstalled with command: {silentCommand}");
                        Console.WriteLine($"Successfully uninstalled with command: {silentCommand}");
                        break;
                    }
                    else
                    {
                        _logger.LogError($"Uninstallation failed with command: {silentCommand}");
                        Console.WriteLine($"Uninstallation failed with command: {silentCommand}");
                    }
                }

                return uninstallSucceeded;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during uninstallation: {ex.Message}");
                Console.WriteLine($"Error during uninstallation: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> RunProcessAsync(string fileName, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.Verb = "runas"; // Administrator huquqlari bilan ishga tushirish

                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit());

                    Console.WriteLine($"Exit Code: {process.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine($"Output: {output}");
                    if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"Error: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RunProcessAsync error: {ex.Message}");
                Console.WriteLine($"RunProcessAsync error: {ex.Message}");
                return false;
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