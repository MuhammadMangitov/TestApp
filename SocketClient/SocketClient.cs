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
using DgzAIOWindowsService;
using DgzAIO.HttpService;
using DBHelper;
using SocketClient.Models;
using ApplicationMonitor;
using System.ServiceModel;

namespace SocketClient  
{
    public class SocketClient
    {
        private readonly SocketIOClient.SocketIO client;
        private bool isRegistered = false;
        private static readonly HttpClient httpClient = new HttpClient();

        string apiUrl = ConfigurationManagerSocket.SocketSettings.InstallerApiUrl;
        string socketUrl = ConfigurationManagerSocket.SocketSettings.ServerUrl;


        public SocketClient()
        {
            client = new SocketIOClient.SocketIO(socketUrl, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
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

            client.On("connect_error", response =>
            {
                Console.WriteLine("Socket connect_error:");
                Console.WriteLine(response.ToString());
                SQLiteHelper.WriteError($"Socket connect_error: {response}");
            });

            client.On("command", async response =>
            {
                Console.WriteLine($"Received command event: {response}");

                var commandData = response.GetValue<CommandData>();

                Console.WriteLine($"Command: {commandData.command}, App Name: {commandData.name}");
                SQLiteHelper.WriteLog("SocketClient", "RegisterEvents", $"Command: {commandData.command}, App Name: {commandData.name}");

                await HandleAppCommand(commandData);
            });

            client.On("delete_agent", response =>
            {
                Console.WriteLine("Agentni o‘chirish buyrildi.");
                SQLiteHelper.WriteLog("SocketClient", "RegisterEvents", "Agentni o‘chirish buyrildi.");

                //var productCode = GetMsiProductCode();
                //SQLiteHelper.WriteLog("SocketClient", "RegisterEvents", $"{productCode}");

                SQLiteHelper.WriteLog("SocketClient", "RegisterEvents", "Agentni o‘chirish so‘rovi xizmatga yuborildi.");
                client.EmitAsync("delete_agent", new { status = "success", message = "Agent o‘chirilmoqda..." });

                SendUninstallToService();
            });
        }

        public async Task<bool> StartSocketListener()
        {
            try
            {   
                string jwtToken = await SQLiteHelper.GetJwtToken();

                if (string.IsNullOrEmpty(jwtToken))
                {
                    Console.WriteLine("Token topilmadi!");
                    return false;
                }


                client.Options.ExtraHeaders = new Dictionary<string, string> { { "authorization", $"Bearer {jwtToken}" } };


                Console.WriteLine($"SocketURL: {socketUrl}");

                await client.ConnectAsync();

                Console.WriteLine($"SocketURL: {socketUrl}");
                Console.WriteLine($"Token header: Bearer {jwtToken}");

                if (!client.Connected)
                {
                    Console.WriteLine("Socket serverga ulanish muvaffaqiyatsiz tugadi!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Socket ulanishda xatolik: {ex.Message}");
                SQLiteHelper.WriteError($"Socket ulanishda xatolik: {ex.Message}");
                return false;
            }
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
                SQLiteHelper.WriteError($"Xatolik: {ex.Message}");
            }
        }


        [ServiceContract]
        interface IAgentService
        {
            [OperationContract]
            void UninstallAgent();
        }
        private void SendUninstallToService()
        {
            Console.WriteLine("Agentni o‘chirish so‘rovi xizmatga yuborilmoqda...");

            var binding = new NetNamedPipeBinding();
            var endpoint = new EndpointAddress("net.pipe://localhost/DgzAIOWindowsService");

            using (var factory = new ChannelFactory<IAgentService>(binding, endpoint))
            {
                var channel = factory.CreateChannel();
                var clientChannel = (IClientChannel)channel;

                bool success = false;

                try
                {
                    channel.UninstallAgent();
                    success = true;
                    Log("Agentni o‘chirish so‘rovi xizmatga yuborildi.");
                }
                catch (EndpointNotFoundException)
                {
                    Console.WriteLine("Xizmat topilmadi, iltimos xizmat ishga tushirilganligini tekshiring.");
                    Log("Xizmat topilmadi, iltimos xizmat ishga tushirilganligini tekshiring.");
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"WCF aloqa xatosi: {ex.Message}");
                    Log($"WCF aloqa xatosi: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                    Log($"Xatolik: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        if (clientChannel.State != CommunicationState.Faulted)
                        {
                            clientChannel.Close();
                        }
                        else
                        {
                            clientChannel.Abort();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Channelni yopishda xatolik: {ex.Message}");
                        Log($"Channelni yopishda xatolik: {ex.Message}");
                    }

                    if (success)
                    {
                        Console.WriteLine("Agentni o‘chirish so‘rovi xizmatga muvaffaqiyatli yuborildi.");
                        Log("Agentni o‘chirish so‘rovi xizmatga muvaffaqiyatli yuborildi.");
                    }
                }
            }
        }
        private void Log(string message)
        {
            File.AppendAllText(@"C:\Logs\SocketLog.txt", $"{DateTime.Now} - {message}\n");
        }
       
        private async Task<bool> DownloadAndInstallApp(string appName, string command)
        {
            try
            {
                string jwtToken = await SQLiteHelper.GetJwtToken();
                if (string.IsNullOrEmpty(jwtToken)) return false;

                Console.WriteLine($"Install app uchun token: {jwtToken}");

                string requestUrl = $"{apiUrl}{appName}";
                Console.WriteLine($"Install app uchun url: {requestUrl}");
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{appName}");
                //string savePath = Path.Combine(Path.GetTempPath(), $"{appName}");


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
                SQLiteHelper.WriteError($"{command.ToUpper()} xatosi: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> DownloadFileAsync(string url, string savePath, string jwtToken)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                Console.WriteLine("Download response: " + response.StatusCode);

                if (!response.IsSuccessStatusCode) return false;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                Console.WriteLine("file keldi");
                return File.Exists(savePath);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download xatosi: {ex.Message}");
                SQLiteHelper.WriteError($"Download xatosi: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> InstallApplicationAsync(string installerPath)
        {
            bool success = await RunProcessAsync(installerPath, "/silent /verysilent /norestart");

            if (success)
            {
                await SendApplicationForSocketAsync();

                string deleteCommand = $"/C timeout /t 3 & del \"{installerPath}\"";
                Process.Start(new ProcessStartInfo("cmd.exe", deleteCommand) { CreateNoWindow = true, UseShellExecute = false });
            }

            return success;
        }

        private async Task<bool> UninstallApplicationAsync(string appName)
        {
            string uninstallString = GetUninstallString(appName);
            if (string.IsNullOrEmpty(uninstallString)) return false;

            bool succes =  await RunProcessAsync("cmd.exe", $"/C \"{uninstallString} /silent /quiet /norestart\"");
            if (succes) 
            {
                await SendApplicationForSocketAsync();
            }
            return succes;
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
                SQLiteHelper.WriteError($"Dastur yopishda xatolik: {ex.Message}");
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

            try
            {
                foreach (string path in registryPaths)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey == null) continue;

                                    string displayName = subKey.GetValue("DisplayName")?.ToString();
                                    string uninstallString = subKey.GetValue("UninstallString")?.ToString();

                                    if (!string.IsNullOrEmpty(displayName) &&
                                        displayName.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        return uninstallString;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"SubKey `{subKeyName}` o‘qishda xatolik: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry o‘qishda xatolik yuz berdi: {ex.Message}");
                SQLiteHelper.WriteError($"Registry o‘qishda xatolik yuz berdi: {ex.Message}");
            }

            return null;
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
        private async Task EmitResponseAsync(string command, bool success, string appName)
        {
            var result = new
            {
                status = success ? "success" : "error",
                command,
                name = appName
            };

            await client.EmitAsync("response", result);
            SQLiteHelper.WriteLog("SocketClient", "EmitResponseAsync", $"Command: {command}, Status: {result.status}");
        }
        private async Task EmitDeleteResponse(string status, string message)
        {
            await client.EmitAsync("delete_agent", new
            {
                status = status,
                message = message
            });
        }

        public static async Task SendApplicationForSocketAsync()
        {
            Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati olinmoqda...");

            var programs = await ApplicationMonitor.ApplicationMonitor.GetInstalledPrograms();  
            bool success = await ApiClient.SendProgramInfo(programs);  

            if (success)
            {
                Console.WriteLine("Dasturlar ro‘yxati serverga yuborildi.");
                SQLiteHelper.WriteLog("SocketClient", "SendApplicationForSocketAsync", "Dasturlar ro‘yxati serverga yuborildi.");
            }
            else
            {
                Console.WriteLine("Dasturlar ro‘yxatini yuborishda xatolik yuz berdi.");
                SQLiteHelper.WriteLog("SocketClient", "SendApplicationForSocketAsync", "Dasturlar ro‘yxatini yuborishda xatolik yuz berdi.");
            }
        }

    }
}
