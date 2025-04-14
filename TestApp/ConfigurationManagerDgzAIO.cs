using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace DgzAIO
{
    public class ConfigurationManagerDgzAIO
    {
        public static class DgzApi
        {
            public static string SERVER_URL_UPDATE_AGENT => Environment.GetEnvironmentVariable("API_SERVER_URL_UPDATE_AGENT") ?? "http://3.145.147.3:3004/agent/update_agent_info.json";
            public static string DOWNLOAD_URL_AGENT_ZIP => Environment.GetEnvironmentVariable("API_DOWNLOAD_URL_AGENT_ZIP") ?? "http://3.145.147.3:3004/agent/update/";
        }
    }
}
