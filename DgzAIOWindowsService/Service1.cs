using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using DgzAIO; // DgzAIO namespace ni qo'shish

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
            workerThread = new Thread(new ThreadStart(StartDgzAIO));
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void StartDgzAIO()
        {
            try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Users\Muhammad\Desktop\c#_modul\github\SystemMonitorInstaller\TestApp\bin\Debug\DgzAIO.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    Process process = new Process { StartInfo = startInfo };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    System.IO.File.AppendAllText(@"C:\LogDgz\MyServiceLog.txt", output);
                    System.IO.File.AppendAllText(@"C:\LogDgz\MyServiceErrors.txt", error);
                }
                catch (Exception ex)
                {
                    string logPath = @"C:\LogDgz\MyServiceLog.txt";
                    string errorMessage = $"[{DateTime.Now}] Xatolik: {ex.ToString()}\n";
                    System.IO.File.AppendAllText(logPath, errorMessage);
                }
     }
    protected override void OnStop()
    {
            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Abort();
            }
        }
    }
}
