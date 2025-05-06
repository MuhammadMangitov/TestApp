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
    public class ConfigurationManagerApiClient
    {
        public static class ApiConfig
        {
            public static string BaseUrl => Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://d.dev-baxa.me/api/1/agent/create";
            public static string BaseUrlForApps => Environment.GetEnvironmentVariable("API_BASE_URL_APPS") ?? "https://d.dev-baxa.me/api/1/agent/applications";
        }
    }
}