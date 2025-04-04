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

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            workerThread = new Thread(StartDgzAIO)
            {
                IsBackground = true
            };
            workerThread.Start();
        }

        private void StartDgzAIO()
        {
            try
            {
                string serviceDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(serviceDir, "DgzAIO.exe");

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
                string errorMessage = $"[{DateTime.Now}] Xatolik: {ex}\n";
                File.AppendAllText(logPath, errorMessage);
            }
        }

        protected override void OnStop()
        {
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(3000); 
            }
        }
    }
}
