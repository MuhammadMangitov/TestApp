using System;
using System.Management;
using System.Threading.Tasks;

namespace DgzAIO.Services
{
    public static class ProcessorInfo
    {
        public static async Task<string> GetCpuAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            return obj["Name"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CPU haqida ma'lumot olishda xatolik: {ex.Message}");
                }
                return "Noma'lum";
            });
        }

        public static async Task<int> GetCpuCoresAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            return Convert.ToInt32(obj["NumberOfCores"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CPU yadrolari haqida ma'lumot olishda xatolik: {ex.Message}");
                }
                return 0;
            });
        }
    }
}
