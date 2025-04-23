﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient.Interfaces
{
    public interface IApplicationManager
    {
        Task<bool> InstallApplicationAsync(string appName, string command);
        Task<bool> UninstallApplicationAsync(string appName);
        bool CloseApplication(string appName);
        Task SendApplicationForSocketAsync();
    }
}
