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
        private static readonly string LogFilePath = @"C:\Logs\AgentService.log";
        public bool UninstallAgent()
        {
            try
            {
                // 1. DgzAIO.exe jarayonini yopish
                try
                {
                    ProcessStartInfo taskKill = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/IM DgzAIO.exe /F",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process killProcess = Process.Start(taskKill);
                    killProcess.WaitForExit();
                    Log(killProcess.ExitCode == 0 ? "DgzAIO.exe jarayoni yopildi." : "DgzAIO.exe jarayoni topilmadi yoki yopilmadi.");
                }
                catch (Exception ex)
                {
                    Log($"Jarayon yopishda xato: {ex.Message}");
                }

                // 2. Registrdan GUID olish
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Datagaze\DLP", true)
                                  ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Datagaze\DLP", true);

                string guid = null;
                if (key != null)
                {
                    guid = key.GetValue("guid")?.ToString();
                    Log($"Guid: {guid}");
                    key.Close();
                }

                // 3. MSI o‘chirishga urinish
                if (!string.IsNullOrEmpty(guid))
                {
                    guid = guid.Trim('{', '}');
                    string commandArgs = $"/x {{{guid}}} /qn";
                    Log($"commands = msiexec.exe {commandArgs}");

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = commandArgs,
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process process = new Process();
                    process.StartInfo = psi;

                    Log("Uninstall jarayoni boshlanyapti...");
                    process.Start();
                    process.WaitForExit();

                    int exitCode = process.ExitCode;
                    Log($"MsiExec chiqqan kod: {exitCode}");

                    switch (exitCode)
                    {
                        case 0:
                            Log("O'chirish muvaffaqiyatli amalga oshirildi.");
                            break;
                        case 1605:
                            Log($"Ko‘rsatilgan GUID {{{guid}}} ga mos dastur tizimda topilmadi. Qoldiq resurslar tozalanmoqda...");
                            break;
                        default:
                            Log($"O'chirishda xato: Exit code {exitCode}");
                            break;
                    }
                }
                else
                {
                    Log("GUID topilmadi, qoldiq resurslar tozalanmoqda...");
                }

                // 4. Registrni tozalash
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\WOW6432Node\Datagaze\DLP", false);
                    Log("Registr kaliti o‘chirildi: SOFTWARE\\WOW6432Node\\Datagaze\\DLP");
                }
                catch (Exception ex)
                {
                    Log($"Registr kalitini o‘chirishda xato (WOW6432Node): {ex.Message}");
                }

                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Datagaze\DLP", false);
                    Log("Registr kaliti o‘chirildi: SOFTWARE\\Datagaze\\DLP");
                }
                catch (Exception ex)
                {
                    Log($"Registr kalitini o‘chirishda xato: {ex.Message}");
                }

                // 5. Fayl va papkalarni tozalash
                try
                {
                    string installPath = @"C:\Program Files (x86)\DgzAIO";
                    if (Directory.Exists(installPath))
                    {
                        foreach (var file in Directory.GetFiles(installPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        Directory.Delete(installPath, true);
                        Log($"O‘rnatish papkasi o‘chirildi: {installPath}");
                    }
                    else
                    {
                        Log($"O‘rnatish papkasi topilmadi: {installPath}");
                    }

                    string dbPath = @"C:\ProgramData\DgzAIO";
                    if (Directory.Exists(dbPath))
                    {
                        foreach (var file in Directory.GetFiles(dbPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        Directory.Delete(dbPath, true);
                        Log($"Ma’lumotlar bazasi papkasi o‘chirildi: {dbPath}");
                    }
                    else
                    {
                        Log($"Ma’lumotlar bazasi papkasi topilmadi: {dbPath}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Fayl/papka o‘chirishda xato: {ex.Message}");
                }

                // 6. MSI keshini tozalash
                if (!string.IsNullOrEmpty(guid))
                {
                    try
                    {
                        string installerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
                        string sourceHashFile = Path.Combine(installerPath, $"SourceHash{{{guid}}}");
                        if (File.Exists(sourceHashFile))
                        {
                            File.SetAttributes(sourceHashFile, FileAttributes.Normal);
                            File.Delete(sourceHashFile);
                            Log($"MSI kesh fayli o‘chirildi: {sourceHashFile}");
                        }
                        else
                        {
                            Log($"MSI kesh fayli topilmadi: {sourceHashFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"MSI kesh faylini o‘chirishda xato: {ex.Message}");
                    }
                }

                // 7. Rejalashtirilgan vazifani o‘chirish
                try
                {
                    string taskName = "DgzAIO";
                    ProcessStartInfo taskDelete = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/Delete /TN \"{taskName}\" /F",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process deleteProcess = Process.Start(taskDelete);
                    deleteProcess.WaitForExit();
                    Log(deleteProcess.ExitCode == 0 ? $"Rejalashtirilgan vazifa {taskName} o‘chirildi." : $"Rejalashtirilgan vazifa {taskName} topilmadi yoki o‘chirilmadi.");
                }
                catch (Exception ex)
                {
                    Log($"Rejalashtirilgan vazifani o‘chirishda xato: {ex.Message}");
                }

                Log("UninstallAgent muvaffaqiyatli yakunlandi.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Uninstall jarayonida xato: {ex.Message}");
                return false;
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
        
    }
}