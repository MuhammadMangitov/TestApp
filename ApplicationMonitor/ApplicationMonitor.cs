using DBHelper;
using DgzAIO.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationMonitor
{
    public class ApplicationMonitor
    {
        public static async Task<List<ProgramDetails>> GetInstalledPrograms()
        {
            var programs = new List<ProgramDetails>();
            var seenPrograms = new HashSet<string>();

            string[] registryKeysLocalMachine = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            string[] registryKeysCurrentUser = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in registryKeysLocalMachine)
            {
                await GetProgramsFromRegistry(Registry.LocalMachine, keyPath, programs, seenPrograms);
            }

            foreach (var keyPath in registryKeysCurrentUser)
            {
                await GetProgramsFromRegistry(Registry.CurrentUser, keyPath, programs, seenPrograms);
            }

            return programs;
        }

        private static async Task GetProgramsFromRegistry(RegistryKey rootKey, string keyPath,
            List<ProgramDetails> programs, HashSet<string> seenPrograms)
        {
            try
            {
                using (var key = rootKey.OpenSubKey(keyPath))
                {
                    if (key == null) return;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                string name = subKey?.GetValue("DisplayName")?.ToString();
                                if (string.IsNullOrEmpty(name) || seenPrograms.Contains(name)) continue;
                                seenPrograms.Add(name);

                                if (subKey?.GetValue("NoDisplay") is int noDisplay && noDisplay == 1) continue;
                                if (subKey?.GetValue("SystemComponent") is int systemComponent && systemComponent == 1) continue;
                                if (subKey?.GetValue("ReleaseType")?.ToString()?.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                                if (subKey?.GetValue("ParentKeyName")?.ToString()?.Equals("OperatingSystem", StringComparison.OrdinalIgnoreCase) == true) continue;

                                string version = subKey?.GetValue("DisplayVersion")?.ToString();
                                string installLocation = subKey?.GetValue("InstallLocation")?.ToString();
                                bool isWindowsInstaller = subKey?.GetValue("WindowsInstaller") is int installer && installer == 1;
                                object registrySize = subKey?.GetValue("EstimatedSize");

                                int? size = await GetProgramSizeSmartAsync(name, installLocation, registrySize);

                                programs.Add(new ProgramDetails
                                {
                                    Name = name,
                                    Size = size,
                                    Type = isWindowsInstaller ? "Windows Installer" : "User",
                                    InstalledDate = ParseInstallDate(subKey?.GetValue("InstallDate")?.ToString()),
                                    Version = version
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Xatolik yuz berdi: {ex.Message}");
                            SQLiteHelper.WriteError($"Registry o‘qishda xatolik: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry o‘qishda xatolik: {ex.Message}");
                SQLiteHelper.WriteError($"Registry o‘qishda xatolik: {ex.Message}");
            }
        }


        private static async Task<int?> GetProgramSizeSmartAsync(string programName, string installLocation, object registrySize)
        {
            if (registrySize != null)
            {
                return Convert.ToInt32(registrySize) / 1024;
            }

            int? wmiSize = await Task.Run(() => GetProgramSizeWMI(programName));
            if (wmiSize.HasValue)
                return wmiSize;

            return await GetProgramSizeAsync(installLocation);
        }

        private static int? GetProgramSizeWMI(string programName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, EstimatedSize FROM Win32_Product"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && name.Equals(programName, StringComparison.OrdinalIgnoreCase))
                        {
                            object sizeObj = obj["EstimatedSize"];
                            if (sizeObj != null)
                            {
                                return Convert.ToInt32(sizeObj) / 1024; // KB → MB
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                
            }
            return null;
        }

        private static async Task<int?> GetProgramSizeAsync(string installLocation)
        {
            if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                return null;

            try
            {
                long size = await Task.Run(() => Directory.EnumerateFiles(installLocation, "*.*", SearchOption.AllDirectories)
                                           .AsParallel()
                                           .Select(f => new FileInfo(f).Length)
                                           .Sum());

                return (int?)(size / 1024 / 1024); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fayllar hajmini hisoblashda xato: {ex.Message}");
                SQLiteHelper.WriteError($"Fayllar hajmini hisoblashda xato: {ex.Message}");
                return null;
            }
        }

        private static DateTime? ParseInstallDate(string installDate)
        {
            if (DateTime.TryParseExact(installDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            return null;
        }
    }

    public static class RegistryExtensions
    {
        public static void TryGetValue(this RegistryKey key, string name, out string result)
        {
            result = key?.GetValue(name)?.ToString();
        }

        public static int GetIntValue(this RegistryKey key, string name)
        {
            return key?.GetValue(name) is int value ? value : 0;
        }

        public static string GetStringValue(this RegistryKey key, string name)
        {
            return key?.GetValue(name)?.ToString() ?? "";
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool EqualsIgnoreCase(this string source, string toCompare)
        {
            return string.Equals(source, toCompare, StringComparison.OrdinalIgnoreCase);
        }
    }
}