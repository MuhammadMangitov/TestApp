using Microsoft.Win32;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DgzAIO.HttpService;
using DBHelper;
using SocketClient.Models;

namespace SocketClient
{
    public class SocketClient
    {
        private readonly SocketIOClient.SocketIO client;
        private bool isRegistered = false;
        private static readonly HttpClient httpClient = new HttpClient();

        public SocketClient()
        {
            string socketUrl = ConfigurationManager.GetSocketServerUrl();
            client = new SocketIOClient.SocketIO(socketUrl, new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 2000,
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            });

            RegisterEvents();
        }

        private void RegisterEvents()
        {
            client.On("connect", async response =>
            {
                Console.WriteLine("Socket.io muvaffaqiyatli ulandi!");
                if (!isRegistered)
                {
                    await client.EmitAsync("register", "SystemMonitor_Client");
                    isRegistered = true;
                    Console.WriteLine("Client ro‘yxatga olindi.");
                }
            });

            client.On("command", async response =>
            {
                Console.WriteLine($"Received command event: {response}");

                var commandData = response.GetValue<CommandData>();

                Console.WriteLine($"Command: {commandData.command}, App Name: {commandData.name}");

                await HandleAppCommand(commandData);
            });

            client.On("delete_agent", async response =>
            {
                Console.WriteLine("Agentni o‘chirish buyrildi.");
                await DeleteAgentAsync();
            });

        }

        public async Task<bool> StartSocketListener()
        {
            string jwtToken = await SQLiteHelper.GetJwtToken();
            Console.WriteLine($"JWT Token: {jwtToken}");
            if (string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine("Token topilmadi!");
                return false;
            }

            client.Options.ExtraHeaders = new Dictionary<string, string> { { "Authorization", $"Bearer {jwtToken}" } };
            await client.ConnectAsync();
            return client.Connected;
        }

        private async Task HandleAppCommand(CommandData data)
        {
            try
            {
                if (data == null || string.IsNullOrEmpty(data.command))
                {
                    Console.WriteLine("Bo‘sh yoki noto‘g‘ri command!");
                    return;
                }

                string command = data.command.ToLower();
                string appName = data.name ?? "";

                bool success = false;

                switch (command)
                {
                    case "delete_app":
                        success = await UninstallApplicationAsync(appName);
                        break;
                    case "install_app":
                    case "update_app":
                        success = await DownloadAndInstallApp(appName, command);
                        break;
                    default:
                        Console.WriteLine($"Noma'lum command: {command}");
                        return;
                }

                await EmitResponseAsync(command, success, appName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xatolik: {ex.Message}");
            }
        }
        public async Task<bool> DeleteAgentAsync()
        {
            try
            {
                string productCode = GetMsiProductCode();
                if (string.IsNullOrEmpty(productCode))
                {
                    Console.WriteLine("ProductCode topilmadi!");
                    await EmitDeleteResponse("error", "ProductCode topilmadi!");
                    return false;
                }

                Console.WriteLine($"ProductCode: {productCode}");

                await EmitDeleteResponse("in_progress", "Agent o‘chirish jarayoni boshlandi.");

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec",
                        Arguments = $"/x {productCode} /qn /norestart",
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };

                bool started = process.Start();
                if (!started)
                {
                    Console.WriteLine("MSI o‘chirishni boshlashda xatolik!");
                    await EmitDeleteResponse("error", "MSI o‘chirishni boshlashda xatolik yuz berdi!");
                    return false;
                }

                Console.WriteLine("O‘chirish jarayoni boshlandi.");
                await EmitDeleteResponse("success", "Agent o‘chirish jarayoni ishga tushdi.");

                Environment.Exit(0);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"O‘chirishda xatolik: {ex.Message}");
                await EmitDeleteResponse("error", $"Xatolik: {ex.Message}");
                return false;
            }
        }
        private async Task EmitDeleteResponse(string status, string message)
        {
            await client.EmitAsync("delete_agent", new
            {
                status = status,
                message = message
            });
        }   
        public static string GetMsiProductCode()
        {
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            string displayName = GetInstalledAgentName();

            string productCode = FindProductCode(Registry.LocalMachine, uninstallKey, displayName);
            if (!string.IsNullOrEmpty(productCode)) return productCode;

            string wow64Key = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            return FindProductCode(Registry.LocalMachine, wow64Key, displayName);
        }
        private static string GetInstalledAgentName()
        {
            string processName = Process.GetCurrentProcess().ProcessName;
            string agentName = processName.Split('.').FirstOrDefault(); 
            return agentName;
        }
        private static string FindProductCode(RegistryKey rootKey, string subKeyPath, string targetName)
        {
            using (RegistryKey uninstallKey = rootKey.OpenSubKey(subKeyPath))
            {
                if (uninstallKey == null) return null;

                foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using (RegistryKey subKey = uninstallKey.OpenSubKey(subKeyName))
                    {
                        string name = subKey?.GetValue("DisplayName") as string;
                        if (!string.IsNullOrEmpty(name) && name.Contains(targetName))
                        {
                            return subKeyName; // Product Code bu subKeyName
                        }
                    }
                }
            }
            return null;
        }

        private async Task<bool> DownloadAndInstallApp(string appName, string command)
        {
            try
            {
                string jwtToken = await SQLiteHelper.GetJwtToken();
                if (string.IsNullOrEmpty(jwtToken)) return false;

                string apiUrl = ConfigurationManager.GetInstallerApiUrl();
                string requestUrl = $"{apiUrl}/{appName}";
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{appName}");

                bool downloaded = await DownloadFileAsync(requestUrl, savePath, jwtToken);
                if (downloaded && (command != "update_app" || CloseApplication(appName)))
                {
                    return await InstallApplicationAsync(savePath);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{command.ToUpper()} xatosi: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UninstallApplicationAsync(string appName)
        {
            string uninstallString = GetUninstallString(appName);
            if (string.IsNullOrEmpty(uninstallString)) return false;

            return await RunProcessAsync("cmd.exe", $"/C \"{uninstallString} /silent /quiet /norestart\"");
        }

        private async Task<bool> InstallApplicationAsync(string installerPath)
        {
            return await RunProcessAsync(installerPath, "/silent /verysilent /norestart");
        }

        private async Task<bool> RunProcessAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                await Task.Run(() => process.WaitForExit());
                return process.ExitCode == 0;
            }
        }

        private async Task<bool> DownloadFileAsync(string url, string savePath, string jwtToken)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return false;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }

                return File.Exists(savePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download xatosi: {ex.Message}");
                return false;
            }
        }

        private bool CloseApplication(string appName)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(appName))
                {
                    process.Kill();
                    process.WaitForExit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dastur yopishda xatolik: {ex.Message}");
                return false;
            }
        }

        private string GetUninstallString(string appName)
        {
            string[] registryPaths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string path in registryPaths)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key == null) continue;

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                        {
                            string displayName = subKey?.GetValue("DisplayName")?.ToString();
                            string uninstallString = subKey?.GetValue("UninstallString")?.ToString();

                            if (!string.IsNullOrEmpty(displayName) && displayName.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return uninstallString;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async Task EmitResponseAsync(string command, bool success, string appName)
        {
            Console.WriteLine($"Response: send ");

            await client.EmitAsync("response", new { command, status = success ? "success" : "error", name = appName });

            Console.WriteLine($"Response: sent "); 
        }

    }
}
