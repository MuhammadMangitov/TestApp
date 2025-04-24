using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;

namespace DgzAIO.HttpService
{
    public class DbConfigurationManager
    {
        public static IConfiguration Configuration { get; }

        static DbConfigurationManager()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static string GetDbPath()
        {
            string registryPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
            string valueName = "DGZ_A_I_O_DB";
            string dbPath = GetRegistryValue(registryPath, valueName);

            if (dbPath != null)
            {
                Console.WriteLine("Registrdan olingan qiymat: " + dbPath);
            }
            else
            {
                Console.WriteLine("Registrda o'zgaruvchi topilmadi.");
            }
            return dbPath;
            /*string dbPath = Environment.GetEnvironmentVariable("DGZ_A_I_O_DB", EnvironmentVariableTarget.Machine);

            if (string.IsNullOrEmpty(dbPath))
            {
                dbPath = Configuration["DGZ_AIO_DB_PATH"];
            }

            return dbPath;*/
        }
        public static string GetRegistryValue(string keyPath, string valueName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(valueName);
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xatolik: {ex.Message}");
            }

            return null;
        }

    }
}