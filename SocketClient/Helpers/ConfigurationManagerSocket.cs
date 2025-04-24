using SocketClient.Interfaces;
using System.Configuration;

namespace SocketClient.Helpers
{
    public class ConfigurationManagerSocket : IConfiguration
    {
        public static class SocketSettings
        {
            public static string InstallerApiUrl => "https://datagaze-platform-9cab2c02bc91.herokuapp.com/api/1/agent/files/";
            public static string ServerUrl => "wss://datagaze-platform-9cab2c02bc91.herokuapp.com/agent";
        }

        public string GetApiUrl()
        {
            return SocketSettings.InstallerApiUrl;
        }

        public string GetSocketUrl()
        {
            return SocketSettings.ServerUrl;
        }
    }
}