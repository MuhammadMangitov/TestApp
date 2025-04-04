using DBHelper;
using System;
using System.Management;
using System.Threading.Tasks;

namespace DgzAIO.Services
{
    public static class MemoryInfo
    {
        public static async Task<long> GetRamAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            return Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RAM ma'lumotlarini olishda xatolik: {ex.Message}");
                    SQLiteHelper.WriteError($"RAM ma'lumotlarini olishda xatolik: {ex.Message}");
                }
                return 0;
            });
        }
    }
}
