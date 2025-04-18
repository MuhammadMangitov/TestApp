using Microsoft.Win32;
using System;
using System.ComponentModel.Design;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace DgzAIOWindowsService
{
    public class AgentService : IAgentService
    {

        private static readonly string LogFilePath = @"C:\Logs\AgentService.log";

        [DllImport("msi.dll", CharSet = CharSet.Auto)]
        private static extern int MsiQueryProductState(string productCode);

        public void UninstallAgent()
        {
            try
            {
                string guid = ReadProductGUID();
                if (string.IsNullOrEmpty(guid))
                {
                    Log("[Uninstall] GUID topilmadi. Uninstall bekor qilindi.");
                    return;
                }

                // MSI API orqali mahsulot holatini tekshiramiz.
                // Agar MsiQueryProductState() manfiy (0 dan kichik) bo‘lsa, mahsulot o‘rnatilmagan.
                int state = MsiQueryProductState(guid);
                Log($"[Uninstall] Mahsulot holati: {state} (0 - o‘rnatilgan, 1 - o‘rnatilmagan, -1 - xato).");
                if (state < 0)
                {
                    Log($"[Uninstall] Mahsulot o‘rnatilmagan (state={state}). Uninstall buyrug‘i bajarilmaydi.");
                    return;
                }
                string command = $"/c \"MsiExec.exe /x{guid} /qn\"";
                Log($"commands = msiexec.exe -- {command}");

                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process process = new Process();
                process.StartInfo = psi;

                Log("Uninstall jarayoni boshlanyapti...");
                Log($"{psi.Arguments.ToString()}");
                process.Start();

                if (process != null)
                {
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    if (exitCode == 0)
                    {
                        Log("[Uninstall] Uninstallation muvaffaqiyatli yakunlandi.");
                    }
                    else if (exitCode == 1605)
                    {
                        Log("[Uninstall] Mahsulot topilmadi (error 1605), ehtimol allaqachon o‘chirib tashlangan.");
                    }
                    else
                    {
                        Log($"[Uninstall] Uninstallation xatoligi: Exit Code {exitCode}");
                    }
                }
                else
                {
                    Log("[Uninstall] BAT fayl ishga tushmadi.");
                }

                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                Log("[Uninstall] Xatolik: " + ex.Message);
            }
        }
        private string ReadProductGUID()
        {
            string guid = null;
            RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Datagaze\DLP", false);
            if (key != null)
            {
                guid = key.GetValue("guid")?.ToString();
                Log("[Uninstall] (64-bit) GUID: " + guid);
                key.Close();
            }

            if (string.IsNullOrEmpty(guid))
            {
                key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey(@"SOFTWARE\Datagaze\DLP", false);
                if (key != null)
                {
                    guid = key.GetValue("guid")?.ToString();
                    Log("[Uninstall] (32-bit) GUID: " + guid);
                    key.Close();
                }
            }

            if (!string.IsNullOrEmpty(guid) && !guid.StartsWith("{"))
            {
                guid = "{" + guid + "}";
            }
            return guid;
        }

        private void StopService()
        {
            try
            {
                ServiceController sc = new ServiceController("DgzAIOService");
                if (sc.Status != ServiceControllerStatus.Stopped &&
                    sc.Status != ServiceControllerStatus.StopPending)
                {
                    sc.Stop();
                    Log("[Uninstall] Service to'xtatish komandasi yuborildi.");
                }
            }
            catch (Exception ex)
            {
                Log("[Uninstall] Service to'xtatishda xato: " + ex.Message);
            }
        }

        /*public void UninstallAgent()
        {
            try
            {
                string guid = ReadProductGUID();
                if (string.IsNullOrEmpty(guid))
                {
                    Log("[Uninstall] GUID topilmadi. Uninstall bekor qilindi.");
                    return;
                }

                // MSI API orqali mahsulot holatini tekshiramiz.
                // Agar MsiQueryProductState() manfiy (0 dan kichik) bo‘lsa, mahsulot o‘rnatilmagan.
                int state = MsiQueryProductState(guid);
                Log($"[Uninstall] Mahsulot holati: {state} (0 - o‘rnatilgan, 1 - o‘rnatilmagan, -1 - xato).");
                if (state < 0)
                {
                    Log($"[Uninstall] Mahsulot o‘rnatilmagan (state={state}). Uninstall buyrug‘i bajarilmaydi.");
                    return;
                }

                string dirPath = @"C:\ProgramData";
                string batPath = Path.Combine(dirPath, "uninstall_agent_1704.bat");
                string logPath = Path.Combine(dirPath, "uninstall1704.log");

                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                string batContent = $@"@echo off
echo Uninstalling product with GUID {guid}
timeout /t 3
msiexec /x {guid} /qn /l*v ""{logPath}""
";
                File.WriteAllText(batPath, batContent);
                Log("[Uninstall] BAT fayl yaratilgan: " + batPath);

                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"  // Administrator huquqlarini talab qiladi
                };

                Process process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    if (exitCode == 0)
                    {
                        Log("[Uninstall] Uninstallation muvaffaqiyatli yakunlandi.");
                    }
                    else if (exitCode == 1605)
                    {
                        Log("[Uninstall] Mahsulot topilmadi (error 1605), ehtimol allaqachon o‘chirib tashlangan.");
                    }
                    else
                    {
                        Log($"[Uninstall] Uninstallation xatoligi: Exit Code {exitCode}");
                    }
                }
                else
                {
                    Log("[Uninstall] BAT fayl ishga tushmadi.");
                }

                Thread.Sleep(5000);
                StopService();
            }
            catch (Exception ex)
            {
                Log("[Uninstall] Xatolik: " + ex.Message);
            }
        }*/

        /*public void UninstallAgent()
    {
        string logFile = @"C:\Logs\uninstall1.log";
        string logDir = Path.GetDirectoryName(logFile);

        string batFile = Path.Combine("C:\\Windows\\System32\\", "Uninstall_DgzAIO.bat");

        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
            Log($"Log papkasi yaratildi: {logDir}");
        }

        // To‘g‘ri ProductCode’ni aniqlash
        //guid = GetDgzAIOProductCode() ?? guid;
        RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Datagaze\DLP", false)
                              ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Datagaze\DLP", false);

        string guid = null;
        if (key != null)
        {
            guid = key.GetValue("guid")?.ToString();
            Log($"Guid: {guid}");
            key.Close();
        }
        Log($"Ishlatiladigan GUID: {guid}");

        // BAT faylini yaratish
        string batContent = $@"@echo off
setlocal

:: Log fayli
set LOG_FILE={logFile}

:: ProductCode
set GUID={guid}

:: Log yozish
echo %DATE% %TIME% Uninstall jarayoni boshlanyapti... >> ""%LOG_FILE%""
echo %DATE% %TIME% msiexec.exe /x%GUID% /qn /l*v ""%LOG_FILE%"" >> ""%LOG_FILE%""

:: O‘chirish buyrug‘i
msiexec.exe /x %GUID% /qn /l*v ""%LOG_FILE%""

:: Xato kodini olish
set EXIT_CODE=%ERRORLEVEL%

:: Xato kodini logga yozish
echo %DATE% %TIME% Jarayon yakunlandi. Xato kodi: %EXIT_CODE% >> ""%LOG_FILE%""
if %EXIT_CODE%==0 (
echo %DATE% %TIME% O‘chirish muvaffaqiyatli yakunlandi. >> ""%LOG_FILE%""
) else (
echo %DATE% %TIME% Xato yuz berdi. Xato kodi: %EXIT_CODE% >> ""%LOG_FILE%""
)

endlocal
";

        File.WriteAllText(batFile, batContent);
        Log($"BAT fayli yaratildi: {batFile}");

        // ProcessStartInfo sozlamalari
        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batFile}\"",
            CreateNoWindow = true,
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process process = new Process();
        process.StartInfo = psi;

        Log("Uninstall jarayoni boshlanyapti...");
        Log($"cmd.exe /c \"{batFile}\"");

        try
        {
            process.Start();
            process.WaitForExit();

            int exitCode = process.ExitCode;
            Log($"Jarayon yakunlandi. Xato kodi: {exitCode}");

            if (exitCode == 0)
            {
                Log("O‘chirish muvaffaqiyatli yakunlandi.");
            }
            else
            {
                Log($"Xato yuz berdi. Xato kodi: {exitCode}. Log faylini tekshiring: {logFile}");
            }
        }
        catch (Exception ex)
        {
            Log($"Xato yuz berdi: {ex.Message}");
        }
    }*/

        /*public void UninstallAgent()
        {
            string scriptContent = @"
    $logPath = ""C:\Windows\Temp\uninstall_log.txt""
    ""Uninstall started at $(Get-Date)"" | Out-File -Append $logPath

    $app = Get-CimInstance -ClassName Win32_Product | Where-Object { $_.Name -like 'DgzAIO' }
    if ($app) {
        $result = $app | Invoke-CimMethod -MethodName Uninstall
        ""Uninstall result: $($result.ReturnValue)"" | Out-File -Append $logPath
    } else {
        ""Application not found."" | Out-File -Append $logPath
    }
";

            string tempScriptPath = Path.Combine(@"C:\Windows\Temp", "uninstall_agent.ps1");
            File.WriteAllText(tempScriptPath, scriptContent);
            Log($"PowerShell script created at: {tempScriptPath}");
            Log("Uninstall jarayoni boshlanyapti...");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = @"C:\Windows\SysWow64\WindowsPowerShell\v1.0\powershell.exe", // 32-bit PowerShell
                Arguments = "-ExecutionPolicy Bypass -File \"C:\\Windows\\Temp\\uninstall_agent.ps1\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Verb = "runas"  // Administrator sifatida ishga tushirish
            };

            Process.Start(psi);
        }*/

        /*public void UninstallAgent()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Datagaze\DLP", true)
                                  ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Datagaze\DLP", true);

                string guid = null;
                if (key != null)
                {
                    guid = key.GetValue("guid")?.ToString();
                    Log($"Guid: {guid}");
                    key.Close();
                }

                if (!string.IsNullOrEmpty(guid))
                {
                    string command = $"/c \"MsiExec.exe /x{guid} /qn\"";
                    Log($"commands = msiexec.exe -- {command}");

                    ProcessStartInfo psi = new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process process = new Process();
                    process.StartInfo = psi;

                    Log("Uninstall jarayoni boshlanyapti...");
                    Log($"{psi.Arguments.ToString()}");
                    process.Start();

                    process.WaitForExit();

                    int exitCode = process.ExitCode;
                    Log($"MsiExec chiqqan kod: {exitCode}");
                }
                else
                {
                    Log("GUID topilmadi, qoldiq resurslar tozalanmoqda...");
                }
            }
            catch (Exception ex)
            {
                Log($"Uninstall jarayonida xato: {ex.Message}");
            }
        }*/

        /*        public bool UninstallAgent()
       {
           try
           {
               ServiceController[] services = ServiceController.GetServices();
               bool serviceExists = services.Any(s => s.ServiceName == "DgzAIOService");

               if (!serviceExists)
               {
                   Log("DgzAIOWindowsService xizmati topilmadi.");
                   return false;
               }
               ServiceController sc = new ServiceController("DgzAIOService");
               if (sc.Status != ServiceControllerStatus.Stopped)
               {
                   sc.Stop();
                   sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                   Log("DgzAIOWindowsService xizmati to‘xtatildi.");
               }
           }
           catch (Exception ex)
           {
               Log($"Xizmatni to‘xtatishda xato: {ex.Message}");
           }

           try
           {
               ProcessStartInfo psi = new ProcessStartInfo
               {
                   FileName = "sc",
                   Arguments = $"delete DgzAIOService",
                   CreateNoWindow = true,
                   UseShellExecute = false
               };
               var process = Process.Start(psi);
               process.WaitForExit();
               Log("DgzAIOWindowsService tizimdan o‘chirildi.");
           }
           catch (Exception ex)
           {
               Log($"Xizmatni tizimdan o‘chirishda xato: {ex.Message}");
           }


           try
           {
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

               RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Datagaze\DLP", true)
                                 ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Datagaze\DLP", true);

               string guid = null;
               if (key != null)
               {
                   guid = key.GetValue("guid")?.ToString();
                   Log($"Guid: {guid}");
                   key.Close();
               }

               if (!string.IsNullOrEmpty(guid))
               {
                   guid = guid.Trim('{', '}');
                   commandArgs = $"/x {{{guid}}} /qn";
                   string command = $"/c \"MsiExec.exe /x {guid} /qn\"";
                   Log($"commands = msiexec.exe {command}");

                   ProcessStartInfo psi = new ProcessStartInfo
                   {
                       FileName = "cmd.exe",
                       Arguments = command,
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
               }
               else
               {
                   Log("GUID topilmadi, qoldiq resurslar tozalanmoqda...");
               }

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
               try
               {
                   string batFilePath = @"C:\Users\Muhammad\Desktop\uninstall_agent.bat";

                   string[] batCommands = {
                               "@echo off",
                               "echo DgzAIOService xizmatini to‘xtatish va o‘chirish...",
                               "sc stop DgzAIOService",
                               "sc delete DgzAIOService",
                               "echo DgzAIOWindowsService.exe fayli orqali xizmatni o‘chirish (agar kerak bo‘lsa)...",
                               "echo Xizmat to‘liq o‘chirildi." ,
                               "pause"
                           };

                   File.WriteAllLines(batFilePath, batCommands, Encoding.UTF8);
                   Log($"Bat fayl yaratildi: {batFilePath}");

                   ProcessStartInfo psi = new ProcessStartInfo
                   {
                       FileName = batFilePath,
                       UseShellExecute = true, // CMD oynada ochiladi
                       Verb = "runas" // Admin sifatida ishga tushuriladi
                   };

                   Process.Start(psi);
                   Log("uninstall_service.bat ishga tushirildi.");
               }
               catch (Exception ex)
               {
                   Log($"Bat fayl yaratish yoki ishga tushirishda xato: {ex.Message}");
               }


               Log("UninstallAgent muvaffaqiyatli yakunlandi.");
               return true;
           }
           catch (Exception ex)
           {
               Log($"Uninstall jarayonida xato: {ex.Message}");
               return false;
           }

       }*/

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
            string logDir = @"C:\Logs";
            string tempDir = Path.Combine("C:\\ProgramData\\DgzAIO", "DgzAIO_Update");
            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DgzAIO");
            string batFilePath = Path.Combine("C:\\ProgramData\\DgzAIO", "update_dgzaio.bat");
            string logFile = Path.Combine(logDir, "UpdateLog.txt");

            try
            {
                Directory.CreateDirectory(logDir);

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                ZipFile.ExtractToDirectory(zipPath, tempDir);

                using (StreamWriter sw = new StreamWriter(batFilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("@echo off");
                    sw.WriteLine($"echo [{DateTime.Now}] Yangilanish boshlandi. >> \"{logFile}\"");

                    // Servis va dastur to‘xtatilmoqda
                    sw.WriteLine("echo Servis to'xtatilmoqda... >> \"" + logFile + "\"");
                    sw.WriteLine("net stop DgzAIOService >> \"" + logFile + "\"");

                    sw.WriteLine("echo Asosiy dastur to'xtatilmoqda... >> \"" + logFile + "\"");
                    sw.WriteLine("taskkill /F /IM DgzAIO.exe >> \"" + logFile + "\"");

                    // Servis to‘xtashini kutish (majburiy emas, lekin tavsiya etiladi)
                    sw.WriteLine(":waitloop");
                    sw.WriteLine("sc query DgzAIOService | find \"RUNNING\" >nul");
                    sw.WriteLine("if %errorlevel%==0 (");
                    sw.WriteLine("  timeout /t 1 >nul");
                    sw.WriteLine("  goto waitloop");
                    sw.WriteLine(")");

                    sw.WriteLine("timeout /t 2 >nul");

                    // Fayllarni nusxalash
                    foreach (string sourceFile in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = GetRelativePath(tempDir, sourceFile);
                        string destFile = Path.Combine(targetDir, relativePath);
                        string destDir = Path.GetDirectoryName(destFile);

                        sw.WriteLine($"if not exist \"{destDir}\" mkdir \"{destDir}\"");
                        sw.WriteLine($"copy /Y \"{sourceFile}\" \"{destFile}\" >> \"{logFile}\"");
                    }

                    // Vaqtinchalik fayllarni o‘chirish
                    /*sw.WriteLine($"echo Vaqtinchalik fayllar o'chirilmoqda... >> \"{logFile}\"");
                    sw.WriteLine($"del /F /Q \"{zipPath}\" >> \"{logFile}\"");
                    sw.WriteLine($"rmdir /S /Q \"{tempDir}\" >> \"{logFile}\"");*/

                    // Servisni ishga tushirish
                    sw.WriteLine("echo Servis ishga tushirilmoqda... >> \"" + logFile + "\"");
                    sw.WriteLine("net start DgzAIOService >> \"" + logFile + "\"");

                    sw.WriteLine($"echo [{DateTime.Now}] Yangilanish tugadi. >> \"{logFile}\"");
                    sw.WriteLine("exit");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = batFilePath,
                    UseShellExecute = true,
                    Verb = "runas" // Admin huquqda ishga tushadi
                });

                File.AppendAllText(Path.Combine(logDir, "MyServiceLog.txt"),
                    $"[{DateTime.Now}] .bat fayl yaratildi va ishga tushirildi: {batFilePath}\n");
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

                File.Copy(sourceFile, destFile, true); 
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