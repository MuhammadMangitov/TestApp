using System;
using System.Threading.Tasks;
using ComputerInformation.Models;
using DgzAIO.Services;

namespace ComputerInformation
{
    public static class ComputerInfo
    {
        public static async Task<ComputerInfoDetails> GetComputerInfoAsync()
        {
            var info = new ComputerInfoDetails
            {
                HostName = Environment.MachineName,
                OperationSystem = Environment.OSVersion.ToString(),
                Platform = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                BuildNumber = Environment.OSVersion.Version.Build.ToString(),
                Version = Environment.OSVersion.Version.ToString(),
                Ram = await MemoryInfo.GetRamAsync(),
                CPU = await ProcessorInfo.GetCpuAsync(),
                Cores = await ProcessorInfo.GetCpuCoresAsync(),
                NetworkAdapters = await NetworkInfo.GetNetworkAdaptersAsync(),
                Disks = await DiskInfo.GetDisksAsync()
            };

            return info;
        }
    }
}
