using Microsoft.Win32;
using System;
using System.ComponentModel.Design;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;

namespace DgzAIOWindowsService
{
    public class AgentService : IAgentService
    {
        private static readonly string LogFilePath = @"C:\Logs\AgentService.log"; // Log fayli joylashuvi

        public bool UninstallAgent()
        {
            try
            {
                Log("Agentni o‘chirish jarayoni boshlandi.");

                // 1. Xizmatni to‘xtatish
                /*string serviceName = "DgzAIO Windows Service"; // Xizmat nomini o‘zgartiring
                StopService(serviceName);*/
                Log("..........");  

                bool isUninstalled = UninstallMsiFromRegistry();
                if (!isUninstalled)
                {
                    Log("MSI dasturini o‘chirishda muammo yuz berdi.");
                    return false;
                }

                CleanupFiles();

                Log("Agent muvaffaqiyatli o‘chirildi.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Xatolik yuz berdi: {ex.Message}");
                return false;
            }
        }

        private void StopService(string serviceName)
        {
            try
            {
                using (ServiceController service = new ServiceController(serviceName))
                {
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        Log($"{serviceName} xizmatini to‘xtatish...");
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        Log($"{serviceName} xizmati to‘xtatildi.");
                    }
                    else
                    {
                        Log($"{serviceName} xizmati allaqachon to‘xtatilgan.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Xizmatni to‘xtatishda xato: {ex.Message}");
            }
        }

        private bool UninstallMsiFromRegistry()
        {
            try
            {
                Log("MSI o‘chirish jarayoni boshlandi.");
                string appName = "DgzAIO"; 

                RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstallKey == null)
                {
                    uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                }

                if (uninstallKey == null)
                {
                    Log("Uninstall registry bo‘limi topilmadi.");
                    return false;
                }

                // Barcha kalitlarni tekshirish
                string guid = null;
                foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                {
                    using (RegistryKey subKey = uninstallKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;

                        string displayName = subKey.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrEmpty(displayName) && displayName.Contains(appName))
                        {
                            // Dastur topildi, guid ni olish
                            string uninstallString = subKey.GetValue("UninstallString")?.ToString();
                            if (!string.IsNullOrEmpty(uninstallString))
                            {
                                // UninstallString dan guid ni ajratib olish (MsiExec.exe /X{guid} shaklida bo‘ladi)
                                var match = System.Text.RegularExpressions.Regex.Match(uninstallString, @"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}");
                                if (match.Success)
                                {
                                    guid = match.Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                uninstallKey.Close();

                if (string.IsNullOrEmpty(guid))
                {
                    Log($"Dastur topilmadi yoki GUID aniqlanmadi: {appName}");
                    return false;
                }

                // MSI dasturini o‘chirish
                string commandArgs = $"/c MsiExec.exe /x {guid} /qn";
                Log($"MSI o‘chirilmoqda, command: {commandArgs}");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = commandArgs,
                    Verb = "runas",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Log("MSI muvaffaqiyatli o‘chirildi.");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Log($"MSI o‘chirishda xato, exit code: {process.ExitCode}, xabar: {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"MSI o‘chirishda xato: {ex.Message}");
                return false;
            }
        }

        private void CleanupFiles()
        {
            try
            {
                string agentFolder = @"C:\Program Files\DgzAIOAgent"; // Agent papkasini o‘zgartiring
                if (Directory.Exists(agentFolder))
                {
                    Directory.Delete(agentFolder, true);
                    Log($"{agentFolder} papkasi o‘chirildi.");
                }
            }
            catch (Exception ex)
            {
                Log($"Fayllarni o‘chirishda xato: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            Console.WriteLine(logMessage);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log yozishda xato: {ex.Message}");
            }
        }
        public void UpdateAgent(string zipPath)
        {
            string logDir = @"C:\LogDgz";
            string tempDir = Path.Combine(Path.GetTempPath(), "DgzAIO_Update");
            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DgzAIO");

            try
            {
                Directory.CreateDirectory(logDir);

                // DgzAIO ni to'xtatish
                foreach (var process in Process.GetProcessesByName("DgzAIO"))
                {
                    try
                    {
                        process.CloseMainWindow(); // Yumshoq to'xtatish
                        if (!process.WaitForExit(3000)) // 3 sek kutish
                        {
                            process.Kill(); // Zo'ravon to'xtatish
                            process.WaitForExit(3000);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(Path.Combine(logDir, "MyServiceErrors.txt"),
                            $"[{DateTime.Now}] Jarayon to‘xtatishda xato: {ex.Message}\n");
                    }
                }

                // Zipni TEMP ga chiqarish
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                ZipFile.ExtractToDirectory(zipPath, tempDir);

                // Fayllarni almashtirish
                CopyAllFiles(tempDir, targetDir);

                // ZIP va TEMP papkalarni tozalash
                File.Delete(zipPath);
                Directory.Delete(tempDir, true);

                // Log
                File.AppendAllText(Path.Combine(logDir, "MyServiceLog.txt"),
                    $"[{DateTime.Now}] Yangilanish muvaffaqiyatli: {zipPath}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(logDir, "MyServiceErrors.txt"),
                    $"[{DateTime.Now}] Yangilash xatoligi: {ex.Message}\n");
                throw;
            }
        }

        private void CopyAllFiles(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string sourceFile in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = GetRelativePath(sourceDir, sourceFile);
                string destFile = Path.Combine(targetDir, relativePath);
                string destDir = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(sourceFile, destFile, true); // Almashtirish
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith("\\") ? basePath : basePath + "\\");
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        

        
        private static void LogError(string message)
        {
            string logDir = @"C:\LogDgz";
            string logFilePath = Path.Combine(logDir, "error_log.txt");

            // Katalog mavjudligini tekshirib chiqing, agar mavjud bo'lmasa yaratilsin
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Log yozish
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
        }

    }
}