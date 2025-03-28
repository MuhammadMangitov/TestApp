using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgzAIO.HttpService
{
    public class ConfigurationManager
    {
        public static IConfiguration Configuration { get; }

        static ConfigurationManager()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static string GetBaseUrl() => Configuration["ApiConfig:BaseUrl"];
        public static string GetBaseUrlForApps() => Configuration["ApiConfig:BaseUrlForApps"];
        public static string GetDbPath() => Configuration["DatabaseSettings:DbPath"];
        public static string GetSocketServerUrl() => Configuration["SocketSettings:ServerUrl"];
        public static string GetInstallerApiUrl() => Configuration["SocketSettings:InstallerApiUrl"];
        public static string GetUpdateApiUrl() => Configuration["SocketSettings:UpdateApiUrl"];
    }
}