using SocketClient.Interfaces;
using DBHelper;
using System;

namespace SocketClient.Helpers
{
    public class Logger : Interfaces.ILogger
    {
        public void LogInformation(string message)
        {
            SQLiteHelper.WriteLog("SocketClient", "General", message);
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogError(string message)
        {
            SQLiteHelper.WriteError(message);
            Console.WriteLine($"[ERROR] {message}");
        }
    }
}