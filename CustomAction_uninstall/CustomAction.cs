using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace CustomAction_uninstall
{
    public class CustomActions
    {
        public static Session session;

        public static void Log(string text)
        {
            Console.WriteLine(text);

            try
            {
                string logFilePath = @"C:\ProgramData\DgzAIO\Logs\CustomActionLog.txt";
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {text}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log: {ex.Message}");
            }

            if (session != null)
            {
                session.Log(text);
            }
        }

        [CustomAction]
        public static ActionResult CustomAction_uninstall(Session session)
        {
            CustomActions.session = session;
            session.Log("CustomAction_uninstall boshlandi.");
            Log("CustomAction_uninstall boshlandi.");

            var serviceName = "DgzAIOService";

            try
            {
                ServiceController sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    Log($"Xizmat to‘xtatilyapti: {serviceName}...");
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                }
                Process.Start("sc", $"delete {serviceName}").WaitForExit();
                Log($"Xizmat o‘chirildi: {serviceName}.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ServiceHelper", ex.Message, EventLogEntryType.Error);
            }

            try
            {
                string processName = "DgzAIO.exe";
                session.Log($"Jarayon tugatilmoqda: {processName}...");

                var taskKill = new Process();
                taskKill.StartInfo.FileName = "taskkill";
                taskKill.StartInfo.Arguments = $"/IM \"{processName}\" /F";
                taskKill.StartInfo.UseShellExecute = false;
                taskKill.StartInfo.CreateNoWindow = true;
                taskKill.Start();
                taskKill.WaitForExit();

                if (taskKill.ExitCode == 0)
                {
                    Log($"{processName} muvaffaqiyatli to‘xtatildi.");
                    session.Log($"{processName} muvaffaqiyatli to‘xtatildi.");
                }
                else
                {
                    Log($"Ogohlantirish: {processName}ni to‘xtatib bo‘lmadi. Ehtimol ishga tushirilmagan.");
                    session.Log($"Ogohlantirish: {processName}ni to‘xtatib bo‘lmadi. Ehtimol ishga tushirilmagan.");
                }

                string installPath = @"C:\Program Files (x86)\DgzAIO";
                session.Log($"O‘rnatish papkasi tekshirilmoqda: {installPath}");

                if (Directory.Exists(installPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(installPath, "*", SearchOption.AllDirectories))
                            File.SetAttributes(file, System.IO.FileAttributes.Normal);

                        Directory.Delete(installPath, true);
                        session.Log($"O‘rnatish papkasi o‘chirildi: {installPath}.");
                        Log($"O‘rnatish papkasi o‘chirildi: {installPath}.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"Ruxsat berilmadi: {installPath}ni o‘chirishda {ex.Message}");
                        session.Log($"Ruxsat berilmadi: {installPath}ni o‘chirishda {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Log($"IO xatolik: {installPath}ni o‘chirishda {ex.Message}. Ba'zi fayllar ishlatilmoqda.");
                        session.Log($"IO xatolik: {installPath}ni o‘chirishda {ex.Message}. Ba'zi fayllar ishlatilmoqda.");
                    }
                }
                else
                {
                    Log($"O‘rnatish papkasi topilmadi: {installPath}.");
                    session.Log($"O‘rnatish papkasi topilmadi: {installPath}.");
                }

                string dbPath = @"C:\ProgramData\DgzAIO\DgzAIODb";
                session.Log($"Ma'lumotlar bazasi papkasi tekshirilmoqda: {dbPath}");

                if (Directory.Exists(dbPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(dbPath, "*", SearchOption.AllDirectories))
                            File.SetAttributes(file, System.IO.FileAttributes.Normal);

                        Directory.Delete(dbPath, true);
                        Log($"Ma'lumotlar bazasi papkasi o‘chirildi: {dbPath}.");
                        session.Log($"Ma'lumotlar bazasi papkasi o‘chirildi: {dbPath}.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"Ruxsat berilmadi: {dbPath}ni o‘chirishda {ex.Message}");
                        session.Log($"Ruxsat berilmadi: {dbPath}ni o‘chirishda {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Log($"IO xatolik: {dbPath}ni o‘chirishda {ex.Message}. Ba'zi fayllar ishlatilmoqda.");
                        session.Log($"IO xatolik: {dbPath}ni o‘chirishda {ex.Message}. Ba'zi fayllar ishlatilmoqda.");
                    }
                }
                else
                {
                    Log($"Ma'lumotlar bazasi papkasi topilmadi: {dbPath}.");
                    session.Log($"Ma'lumotlar bazasi papkasi topilmadi: {dbPath}.");
                }

                try
                {
                    string taskName = "DgzAIO";
                    session.Log($"Rejalashtirilgan vazifa o‘chirilyapti: {taskName}...");

                    var taskDelete = new Process();
                    taskDelete.StartInfo.FileName = "schtasks";
                    taskDelete.StartInfo.Arguments = $"/Delete /TN \"{taskName}\" /F";
                    taskDelete.StartInfo.UseShellExecute = false;
                    taskDelete.StartInfo.CreateNoWindow = true;
                    taskDelete.Start();
                    taskDelete.WaitForExit();

                    if (taskDelete.ExitCode == 0)
                    {
                        Log($"Rejalashtirilgan vazifa muvaffaqiyatli o‘chirildi: {taskName}.");
                        session.Log($"Rejalashtirilgan vazifa muvaffaqiyatli o‘chirildi: {taskName}.");
                    }
                    else
                    {
                        Log($"Ogohlantirish: Rejalashtirilgan vazifani o‘chirib bo‘lmadi: {taskName}. Ehtimol mavjud emas.");
                        session.Log($"Ogohlantirish: Rejalashtirilgan vazifani o‘chirib bo‘lmadi: {taskName}. Ehtimol mavjud emas.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Xato: rejalashtirilgan vazifani o‘chirishda {ex.Message}");
                    session.Log($"Xato: rejalashtirilgan vazifani o‘chirishda {ex.Message}");
                }

                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                    {
                        baseKey.DeleteSubKeyTree(@"SOFTWARE\WOW6432Node\Datagaze", throwOnMissingSubKey: false);

                        session.Log(@"Registriyadan 'HKLM\SOFTWARE\WOW6432Node\Datagaze' o‘chirildi.");
                        Log(@"Registriyadan 'HKLM\SOFTWARE\WOW6432Node\Datagaze' o‘chirildi.");
                    }
                }
                catch (Exception ex)
                {
                    session.Log($"Ogohlantirish: Datagaze kalitini o‘chirishda xato: {ex.Message}");
                    Log($"Ogohlantirish: Datagaze kalitini o‘chirishda xato: {ex.Message}");
                }

                session.Log("CustomAction_uninstall muvaffaqiyatli yakunlandi.");
                Log("CustomAction_uninstall muvaffaqiyatli yakunlandi.");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                Log($"XATO: CustomAction_uninstall jarayonida: {ex.Message}");
                session.Log($"XATO: CustomAction_uninstall jarayonida: {ex.Message}");
                return ActionResult.Failure;
            }
        }
    }
}