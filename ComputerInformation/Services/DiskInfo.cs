using ComputerInformation.Models;
using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace DgzAIO.Services
{
    public static class DiskInfo
    {
        public static async Task<List<DiskDetails>> GetDisksAsync()
        {
            return await Task.Run(() =>
            {
                var disks = new List<DiskDetails>();
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var disk = new DiskDetails
                            {
                                DriveName = obj["DeviceID"].ToString(),
                                TotalSize = ConvertBytesToMB(Convert.ToInt64(obj["Size"])),
                                AvailableSpace = ConvertBytesToMB(Convert.ToInt64(obj["FreeSpace"]))
                            };
                            disks.Add(disk);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Disklar haqida ma'lumot olishda xatolik: {ex.Message}");
                }
                return disks;
            });
        }

        private static long ConvertBytesToMB(long bytes)
        {
            return bytes / (1024 * 1024);
        }
    }
}
