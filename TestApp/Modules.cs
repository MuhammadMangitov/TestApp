using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationMonitor;
using DBHelper;
using DgzAIO.HttpService;

namespace DgzAIO
{
    public class Modules
    {
        public static void StartDBHelper()
        {
            Thread thread = new Thread(new ThreadStart(StartDBHelperThread));
            thread.Start();
        }

        private static void StartDBHelperThread()
        {
            try
            {
                if (SQLiteHelper.CreateConnection() == null)
                {
                    Console.WriteLine("Baza ulanishi muvaffaqiyatsiz bo‘ldi.");
                }
                else
                {
                    SQLiteHelper.ClearLogs();
                }

                Console.WriteLine("ClearLogs metodi bajarildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xatolik: {ex.Message}");
            }
        }

        public static void StartApplicationMonitor()
        {
            Thread thread = new Thread(new ThreadStart(StartApplicationMonitorThread));
            thread.Start();
        }
        private static void StartApplicationMonitorThread()
        {
            ApplicationMonitor.ApplicationMonitor monitor = new ApplicationMonitor.ApplicationMonitor();
            
        }


        public static void StartSocketClient()
        {
            Thread thread = new Thread(new ThreadStart(StartSocketClientThread));
            thread.Start();
        }
        private static void StartSocketClientThread()
        {
            SocketClient.SocketClient client = new SocketClient.SocketClient();
            client.Connect();
        }
        
        public static void StartComputerInformation()
        {
            Thread thread = new Thread(new ThreadStart(StartComputerInformationThread));
            thread.Start();
        }
        private static void StartComputerInformationThread()
        {
            
        }


        public static void StartApiClient()
        {
            Thread thread = new Thread(new ThreadStart(StartApiClientThread));
            thread.Start();
        }
        private static void StartApiClientThread() 
        {
            ApiClient client = new ApiClient();
        }

    }
}
