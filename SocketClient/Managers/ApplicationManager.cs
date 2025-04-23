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

        public async Task<bool> UninstallApplicationAsync(string appName)
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
        }
    }
}