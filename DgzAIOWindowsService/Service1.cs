﻿using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;

namespace DgzAIOWindowsService
{
    public partial class Service1 : ServiceBase
    {
        private Thread workerThread;
        private ServiceHost serviceHost;
        private bool isRunning = true; 
        private readonly string serviceDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DgzAIO.exe");

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string projectDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DgzAIO");

            string dbPath = Path.Combine(projectDataPath, "DgzAIODb");
            string logsPath = Path.Combine(projectDataPath, "Logs");

            serviceHost = new ServiceHost(typeof(AgentService));
            serviceHost.AddServiceEndpoint(
                typeof(IAgentService),
                new NetNamedPipeBinding(),
                "net.pipe://localhost/DgzAIOWindowsService");
            serviceHost.Open();

            workerThread = new Thread(MonitorAndStartDgzAIO) { IsBackground = true };
            workerThread.Start();

            Logger.Log("The service and WCF are up and running.");
        }
        private void MonitorAndStartDgzAIO()
        {
            while (isRunning)
            {
                try
                {
                    var processes = Process.GetProcessesByName("DgzAIO");
                    if (processes.Length == 0)
                    {
                        StartDgzAIO();
                        Logger.Log("DgzAIO has been restarted.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Monitoring error: {ex.Message}");
                }

                Thread.Sleep(5000);
            }
        }


        private void StartDgzAIO()
        {
            try
            {
                if (!File.Exists(exePath))
                {
                    Logger.LogError($"DgzAIO.exe not found: {exePath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false, 
                    CreateNoWindow = true, 
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(exePath) 
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"DgzAIO startup error: {ex.Message}");
            }
        }


        protected override void OnStop()
        {
            isRunning = false;

            if (workerThread != null && workerThread.IsAlive)
                workerThread.Join(3000);

            if (serviceHost != null)
                serviceHost.Close();

            Logger.Log("Service stopped.");
        }

    }
}
