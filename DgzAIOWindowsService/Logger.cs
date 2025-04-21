using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgzAIOWindowsService
{
    public static class Logger
    {
        private static readonly string logsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DgzAIO", "Logs");

        private static readonly string logFile = Path.Combine(logsPath, "MyServiceLog.txt");
        private static readonly string errorFile = Path.Combine(logsPath, "MyServiceErrors.txt");

        static Logger()
        {
            try
            {
                if (!Directory.Exists(logsPath))
                    Directory.CreateDirectory(logsPath);
            }
            catch (Exception ex)
            {
                // Agar log katalogini yaratib bo‘lmasa, boshqa yo‘l bilan log yozish kerak (masalan Event Log)
                Console.WriteLine("Log katalogi yaratilmadi: " + ex.Message);
            }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { /* log yozib bo‘lmadi - xatolikni yutamiz */ }
        }

        public static void LogError(string message)
        {
            try
            {
                File.AppendAllText(errorFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}{Environment.NewLine}");
            }
            catch { /* log yozib bo‘lmadi - xatolikni yutamiz */ }
        }
    }
}
