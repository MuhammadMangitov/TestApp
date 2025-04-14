using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient
{
    public static class ConfigurationManagerSocket
    {
        public static class SocketSettings
        {
            public static string ServerUrl => Environment.GetEnvironmentVariable("SOCKET_SERVER_URL") ?? "ws://3.145.147.3:3005";
            public static string InstallerApiUrl => Environment.GetEnvironmentVariable("SOCKET_INSTALLER_API_URL") ?? "http://3.145.147.3:3004/agent/application/download";
        }
    }
}
