using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ApplicationMonitor;
using DBHelper;
using DgzAIO.HttpService;

namespace DgzAIO
{
    public class Modules
    {
        public static void Start()
        {
            StartDBHelper();             
            StartApiClient();            
            StartApplicationMonitor();  
            StartComputerInformation();  
            StartSocketClient();        
        }

        public static void StartComputerInformation()
        {
            Thread thread = new Thread(new ThreadStart(StartComputerInformationThread));
            thread.Start();
        }

        private static void StartComputerInformationThread()
        {
            Console.WriteLine("[Computer Information] Timer boshlanmoqda...");
            StartTimer();
        }

        public static void StartDBHelper()
        {
            Thread thread = new Thread(new ThreadStart(StartDBHelperThread));
            thread.Start();
        }

        private static void StartDBHelperThread()
        {
            Console.WriteLine("[DBHelper] SQLite ulanishi amalga oshirilmoqda...");
            var connection = SQLiteHelper.CreateConnection();
            if (connection != null)
            {
                Console.WriteLine("[DBHelper] SQLite ulanishi muvaffaqiyatli!");
            }
            else
            {
                Console.WriteLine("[DBHelper] Xatolik yuz berdi!");
            }
        }

        public static void StartApplicationMonitor()
        {
            Thread thread = new Thread(new ThreadStart(StartApplicationMonitorThread));
            thread.Start();
        }

        private static void StartApplicationMonitorThread()
        {
            if (SQLiteHelper.ShouldSendProgramInfo())
            {
                Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati API ga yuborilmoqda...");
                SendProgramInfo().Wait();
            }
        }

        public static void StartSocketClient()
        {
            Thread thread = new Thread(new ThreadStart(StartSocketClientThread));
            thread.Start();
        }

        private static async void StartSocketClientThread()
        {
            Console.WriteLine("[Socket Client] Ulanish boshlanmoqda...");

            SocketClient.SocketClient socketManager = new SocketClient.SocketClient();

            bool isConnected = await socketManager.StartSocketListener();
            if (isConnected)
            {
                Console.WriteLine("[Socket Client] Socket.io muvaffaqiyatli ulandi!");
            }
            else
            {
                Console.WriteLine("[Socket Client] Ulanishda xatolik yuz berdi!");
            }
        }

        public static void StartApiClient()
        {
            Thread thread = new Thread(new ThreadStart(StartApiClientThread));
            thread.Start();
        }

        private static async void StartApiClientThread()
        {
            Console.WriteLine("[API Client] JWT token olinmoqda...");
            var (token, statusCode) = await ApiClient.GetJwtTokenFromApi();
            if (!string.IsNullOrEmpty(token))
            {
                SQLiteHelper.InsertJwtToken(token);
                Console.WriteLine("[API Client] JWT token saqlandi!");

                if (statusCode == 201)
                {
                    await SendProgramInfo();
                }
            }
            else
            {
                Console.WriteLine("[API Client] JWT token olishda xatolik yuz berdi!");
            }
        }

        private static async Task SendProgramInfo()
        {
            Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati olinmoqda...");
            var programs = ApplicationMonitor.ApplicationMonitor.GetInstalledPrograms();
            bool success = await ApiClient.SendProgramInfo(programs);

            if (success)
            {
                Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati muvaffaqiyatli jo‘natildi.");
                SQLiteHelper.UpdateLastSentTime(DateTime.UtcNow);
            }
            else
            {
                Console.WriteLine("[Application Monitor] Dasturlar ro‘yxatini jo‘natishda xatolik yuz berdi!");
            }
        }

        private static void StartTimer()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(24)
            };

            timer.Tick += async (sender, args) =>
            {
                Console.WriteLine("[Timer] 24 soat o‘tdi, yangi ma’lumot yuborilmoqda...");
                if (!SQLiteHelper.ShouldSendProgramInfo())
                {
                    Console.WriteLine("[Timer] 24 soat o‘tmaganligi sababli dasturlar ro‘yxati jo‘natilmadi.");
                    return;
                }

                await SendProgramInfo();
            };

            timer.Start();
        }
    }

}
