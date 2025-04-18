/*using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace CustomAction_uninstall
{
    public class CustomActions
    {
        private static Session session;

        private static void Log(string text)
        {
            try
            {
                string logFilePath = "C:\\Logs\\CustomActionLog.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {text}");
                }
            }
            catch (Exception ex)
            {
                session?.Log($"Error writing log: {ex.Message}");
            }

            session?.Log(text);
        }

        [CustomAction]
        public static ActionResult CustomAction_uninstall(Session session)
        {
            CustomActions.session = session;
            session.Log("CustomAction_uninstall started.");
            Log("CustomAction_uninstall started.");

            try
            {
                Process[] processes = Process.GetProcessesByName("DgzAIO");
                foreach (Process process in processes)
                {
                    try
                    {
                        process.CloseMainWindow();
                        process.WaitForExit(5000); // 5 soniya kutish
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Log("Process DgzAIO.exe was forcibly terminated.");
                        }
                        else
                        {
                            Log("Process DgzAIO.exe was gracefully terminated.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error terminating process: {ex.Message}");
                    }
                }

                string taskName = "DgzAIO";
                try
                {
                    var taskDelete = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "schtasks",
                            Arguments = $"/Delete /TN \"{taskName}\" /F",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    taskDelete.Start();
                    taskDelete.WaitForExit();
                    Log(taskDelete.ExitCode == 0
                        ? $"Scheduled task {taskName} deleted."
                        : $"Warning: Could not delete scheduled task {taskName}.");
                }
                catch (Exception ex)
                {
                    Log($"Error deleting scheduled task: {ex.Message}");
                }

                string programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DgzAIO");
                if (Directory.Exists(programDataPath))
                {
                    Log($"Deleting folder: {programDataPath}");
                    try
                    {
                        foreach (string file in Directory.GetFiles(programDataPath, "*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                File.SetAttributes(file, System.IO.FileAttributes.Normal);
                                File.Delete(file);
                                Log($"Deleted file: {file}");
                            }
                            catch (IOException ex)
                            {
                                Log($"Cannot delete {file}: {ex.Message}");
                            }
                        }
                        Directory.Delete(programDataPath, true);
                        Log($"Deleted folder: {programDataPath}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error deleting {programDataPath}: {ex.Message}");
                    }
                }
                else
                {
                    Log($"Folder {programDataPath} does not exist.");
                }

                session.Log("CustomAction_uninstall completed successfully.");
                Log("CustomAction_uninstall completed successfully.");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                Log($"ERROR in CustomAction_uninstall: {ex.Message}");
                session.Log($"ERROR in CustomAction_uninstall: {ex.Message}");
                return ActionResult.Failure;
            }
        }
    }
}*/

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
                string logFilePath = @"C:\Logs\CustomActionLog.txt";
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
            session.Log("CustomAction_uninstall started.");
            Log("CustomAction_uninstall started.");
            
            var serviceName = "DgzAIOService";

            try
            {
                ServiceController sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    Log($"Stopping service {serviceName}...");
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                }
                Process.Start("sc", $"delete {serviceName}").WaitForExit();
                Log($"{serviceName} service deleted.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ServiceHelper", ex.Message, EventLogEntryType.Error);
            }
            try
            {
                string processName = "DgzAIO.exe";
                session.Log($"Attempting to terminate {processName}...");

                var taskKill = new Process();
                taskKill.StartInfo.FileName = "taskkill";
                taskKill.StartInfo.Arguments = $"/IM \"{processName}\" /F";
                taskKill.StartInfo.UseShellExecute = false;
                taskKill.StartInfo.CreateNoWindow = true;
                taskKill.Start();
                taskKill.WaitForExit();

                if (taskKill.ExitCode == 0)
                {
                    Log($"{processName} successfully terminated.");
                    session.Log($"{processName} successfully terminated.");
                }
                else
                {
                    Log($"Warning: Could not terminate {processName}. It may not be running.");
                    session.Log($"Warning: Could not terminate {processName}. It may not be running.");
                }

                string installPath = @"C:\Program Files (x86)\DgzAIO";
                session.Log($"Checking installation folder: {installPath}");

                if (Directory.Exists(installPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(installPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, System.IO.FileAttributes.Normal);
                        }

                        Directory.Delete(installPath, true);
                        session.Log($"Installation folder {installPath} deleted.");
                        Log($"Installation folder {installPath} deleted.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"Access denied when deleting {installPath}: {ex.Message}");
                        session.Log($"Access denied when deleting {installPath}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Log($"IO error when deleting {installPath}: {ex.Message}. Some files may be in use.");
                        session.Log($"IO error when deleting {installPath}: {ex.Message}. Some files may be in use.");
                    }
                }
                else
                {
                    Log($"Installation folder {installPath} does not exist.");
                    session.Log($"Installation folder {installPath} does not exist.");
                }

                string dbPath = @"C:\ProgramData\DgzAIO";
                session.Log($"Checking database folder: {dbPath}");

                if (Directory.Exists(dbPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(dbPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(file, System.IO.FileAttributes.Normal);
                        }

                        Directory.Delete(dbPath, true);
                        Log($"Database folder {dbPath} deleted.");
                        session.Log($"Database folder {dbPath} deleted.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log($"Access denied when deleting {dbPath}: {ex.Message}");
                        session.Log($"Access denied when deleting {dbPath}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Log($"IO error when deleting {dbPath}: {ex.Message}. Some files may be in use.");
                        session.Log($"IO error when deleting {dbPath}: {ex.Message}. Some files may be in use.");
                    }
                }
                else
                {
                    Log($"Database folder {dbPath} does not exist.");
                    session.Log($"Database folder {dbPath} does not exist.");
                }

                string taskName = "DgzAIO";
                session.Log($"Attempting to delete scheduled task {taskName}...");

                var taskDelete = new Process();
                taskDelete.StartInfo.FileName = "schtasks";
                taskDelete.StartInfo.Arguments = $"/Delete /TN \"{taskName}\" /F";
                taskDelete.StartInfo.UseShellExecute = false;
                taskDelete.StartInfo.CreateNoWindow = true;
                taskDelete.Start();
                taskDelete.WaitForExit();

                if (taskDelete.ExitCode == 0)
                {
                    Log($"Scheduled task {taskName} successfully deleted.");
                    session.Log($"Scheduled task {taskName} successfully deleted.");
                }
                else
                {
                    Log($"Warning: Could not delete scheduled task {taskName}. It may not exist.");
                    session.Log($"Warning: Could not delete scheduled task {taskName}. It may not exist.");
                }

                session.Log("CustomAction_uninstall completed successfully.");
                Log("CustomAction_uninstall completed successfully.");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                Log($"ERROR in CustomAction_uninstall: {ex.Message}");
                session.Log($"ERROR in CustomAction_uninstall: {ex.Message}");
                return ActionResult.Failure;
            }
        }
    }
}