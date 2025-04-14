using System;
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
            Directory.CreateDirectory(@"C:\LogDgz");

            serviceHost = new ServiceHost(typeof(AgentService));
            serviceHost.AddServiceEndpoint(
                typeof(IAgentService),
                new NetNamedPipeBinding(),
                "net.pipe://localhost/DgzAIOWindowsService");

            serviceHost.Open();

            workerThread = new Thread(MonitorAndStartDgzAIO)
            {
                IsBackground = true
            };
            workerThread.Start();

            File.AppendAllText(@"C:\LogDgz\MyServiceLog.txt",
                $"[{DateTime.Now}] Xizmat va WCF ishga tushdi\n");
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
                        File.AppendAllText(@"C:\LogDgz\MyServiceLog.txt",
                            $"[{DateTime.Now}] DgzAIO qayta ishga tushirildi\n");
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(@"C:\LogDgz\MyServiceErrors.txt",
                        $"[{DateTime.Now}] Monitoring xatoligi: {ex.Message}\n");
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
                    File.AppendAllText(@"C:\LogDgz\MyServiceErrors.txt",
                        $"[{DateTime.Now}] DgzAIO.exe topilmadi: {exePath}\n");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();
               // process.WaitForExit();
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"C:\LogDgz\MyServiceErrors.txt",
                    $"[{DateTime.Now}] DgzAIO ishga tushirish xatoligi: {ex.Message}\n");
            }
        }


        protected override void OnStop()
        {
            isRunning = false; 

            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(3000);
            }

            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            File.AppendAllText(@"C:\LogDgz\MyServiceLog.txt",
                $"[{DateTime.Now}] Xizmat to‘xtadi\n");
        }
    }
}

/*
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace DgzAIOWindowsService
{
    public partial class Service1 : ServiceBase
    {
        private Thread workerThread;
        private bool isRunning = true; 

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            workerThread = new Thread(WatchDgzAIO) 
            {
                IsBackground = true
            };
            workerThread.Start();
        }

        private void WatchDgzAIO()
        {
            try
            {
                string serviceDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(serviceDir, "DgzAIO.exe");

                while (isRunning)
                {
                    var processes = Process.GetProcessesByName("DgzAIO");

                    if (processes.Length == 0)  
                    {
                        StartDgzAIO(exePath);  
                    }

                    Thread.Sleep(5000);  
                }
            }
            catch (Exception ex)
            {
                string logPath = @"C:\LogDgz\MyServiceErrors.txt";
                string errorMessage = $"[{DateTime.Now}] Xatolik: {ex}\n";
                File.AppendAllText(logPath, errorMessage);
            }
        }

        private void StartDgzAIO(string exePath)
        {
            try
            {
                if (!File.Exists(exePath)) 
                {
                    File.AppendAllText(@"C:\LogDgz\MyServiceErrors.txt",
                        $"[{DateTime.Now}] DgzAIO.exe topilmadi: {exePath}\n");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden  
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit(); 

                File.AppendAllText(@"C:\LogDgz\MyServiceLog.txt", $"[{DateTime.Now}] DgzAIO ishga tushdi\n");
            }
            catch (Exception ex)
            {
                string logPath = @"C:\LogDgz\MyServiceErrors.txt";
                string errorMessage = $"[{DateTime.Now}] DgzAIO ishga tushirishda xatolik: {ex}\n";
                File.AppendAllText(logPath, errorMessage);
            }
        }

        protected override void OnStop()
        {
            isRunning = false; 
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(3000);  
            }
        }
    }

}
*/