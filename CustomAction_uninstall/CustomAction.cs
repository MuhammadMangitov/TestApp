using System;
using System.Diagnostics;
using System.IO;
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

                // 5. Registrni tozalash
                session.Log("Cleaning registry keys...");

                try
                {

                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\WOW6432Node\Datagaze\DLP", false);
                    Log("Registry key deleted: SOFTWARE\\WOW6432Node\\Datagaze\\DLP");
                    session.Log("Registry key deleted: SOFTWARE\\WOW6432Node\\Datagaze\\DLP");
                }
                catch (Exception ex)
                {
                    Log($"Error deleting registry key SOFTWARE\\WOW6432Node\\Datagaze\\DLP: {ex.Message}");
                    session.Log($"Error deleting registry key SOFTWARE\\WOW6432Node\\Datagaze\\DLP: {ex.Message}");
                }

                try
                {

                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Datagaze\DLP", false);
                    Log("Registry key deleted: SOFTWARE\\Datagaze\\DLP");
                    session.Log("Registry key deleted: SOFTWARE\\Datagaze\\DLP");
                }
                catch (Exception ex)
                {
                    Log($"Error deleting registry key SOFTWARE\\Datagaze\\DLP: {ex.Message}");
                    session.Log($"Error deleting registry key SOFTWARE\\Datagaze\\DLP: {ex.Message}");
                }

                // 6. MSI keshini tozalash
                string guid = "4DDE4511-CDAC-4B81-A5D1-880D625B8DD3";
                session.Log($"Cleaning MSI cache for GUID {{{guid}}}...");

                try
                {
                    string installerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
                    string sourceHashFile = Path.Combine(installerPath, $"SourceHash{{{guid}}}");
                    if (File.Exists(sourceHashFile))
                    {

                        File.SetAttributes(sourceHashFile, System.IO.FileAttributes.Normal);
                        File.Delete(sourceHashFile);
                        Log($"MSI cache file deleted: {sourceHashFile}");
                        session.Log($"MSI cache file deleted: {sourceHashFile}");
                    }
                    else
                    {
                        Log($"MSI cache file not found: {sourceHashFile}");
                        session.Log($"MSI cache file not found: {sourceHashFile}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error deleting MSI cache file: {ex.Message}");
                    session.Log($"Error deleting MSI cache file: {ex.Message}");
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