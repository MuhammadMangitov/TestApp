using ComputerInformation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace DgzAIO.Services
{
    public static class NetworkInfo
    {
        public static async Task<List<AdapterDetails>> GetNetworkAdaptersAsync()
        {
            return await Task.Run(() =>
            {
                var adapters = new List<AdapterDetails>();
                try
                {
                    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up &&
                            (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                             nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                        {
                            var adapter = new AdapterDetails
                            {
                                NicName = nic.Name,
                                IpAddress = nic.GetIPProperties().UnicastAddresses.FirstOrDefault()?.Address.ToString(),
                                MacAddress = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2"))),
                                Available = nic.OperationalStatus.ToString()
                            };
                            adapters.Add(adapter);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Tarmoq adapterlari haqida ma'lumot olishda xatolik: {ex.Message}");
                }
                return adapters;
            });
        }
    }
}
