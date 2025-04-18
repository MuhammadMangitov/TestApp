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
            public static string ServerUrl => Environment.GetEnvironmentVariable("SOCKET_SERVER_URL") ?? "wss://datagaze-platform-9cab2c02bc91.herokuapp.com/agent";
            public static string InstallerApiUrl => Environment.GetEnvironmentVariable("SOCKET_INSTALLER_API_URL") ?? "https://datagaze-platform-9cab2c02bc91.herokuapp.com/api/1/agent/files/";
        }
    }
}
