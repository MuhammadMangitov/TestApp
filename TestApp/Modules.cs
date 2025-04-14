using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ApplicationMonitor;
using DBHelper;
using DgzAIO.HttpService;
using Newtonsoft.Json;

namespace DgzAIO
{
    public class Modules
    {
        public static void Start()
        {
            if (StartDBHelper())
            {
                StartApiClient();
                StartApplicationMonitor();
                StartComputerInformation();
            }
            else
            {
                Console.WriteLine("[DBHelper] Baza bilan ulanishda xatolik yuz berdi. Ilova ishga tushmadi.");
            }
        }

        public static bool StartDBHelper()
        {
            Console.WriteLine("[DBHelper] SQLite ulanishi amalga oshirilmoqda...");

            var connection = SQLiteHelper.CreateConnection();

            if (connection != null)
            {
                Console.WriteLine("[DBHelper] SQLite ulanishi muvaffaqiyatli!");

                SQLiteHelper.CreateTablesIfNotExists();

                return true;
            }
            else
            {
                Console.WriteLine("[DBHelper] Xatolik yuz berdi!");
                return false;
            }
        }
        public static void StartComputerInformation()
        {
            Thread thread = new Thread(new ThreadStart(StartComputerInformationThread));
            thread.Start();
        }

        private static void StartComputerInformationThread()
        {
            Console.WriteLine("[Computer Information] Timer boshlanmoqda...");
            SQLiteHelper.WriteLog("Modules", "StartComputerInformationThread", "Timer boshlanmoqda...");
            StartTimer();
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
                SQLiteHelper.WriteLog("Modules", "StartApplicationMonitorThread", "Dasturlar ro‘yxati API ga yuborilmoqda");
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
            try
            {
                Console.WriteLine("[Socket Client] Ulanish boshlanmoqda...");

                var socketManager = new SocketClient.SocketClient();


                bool isConnected = await socketManager.StartSocketListener();


                if (isConnected)
                {
                    SQLiteHelper.WriteLog("Modules", "StartSocketClientThread", "Socket.io muvaffaqiyatli ulandi");
                    Console.WriteLine("[Socket Client] Socket.io muvaffaqiyatli ulandi!");
                }
                else
                {
                    SQLiteHelper.WriteLog("Modules", "StartSocketClientThread", "Socket.io ulanishda xatolik yuz berdi");
                    Console.WriteLine("[Socket Client] Ulanishda xatolik yuz berdi!");
                }
            }
            catch (Exception ex)
            {
                SQLiteHelper.WriteLog("Modules", "StartSocketClientThread", $"Xatolik: {ex.Message}");
                Console.WriteLine($"[Socket Client] Xatolik: {ex.Message}");
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
                SQLiteHelper.WriteLog("Modules", "StartApiClientThread", "JWT token saqlandi");
                Console.WriteLine("[API Client] JWT token saqlandi!");

                if (statusCode == 201)
                {
                    await SendProgramInfo();
                }
            }
            else
            {
                SQLiteHelper.WriteLog("Modules", "StartApiClientThread", "JWT token olishda xatolik yuz berdi");
                Console.WriteLine("[API Client] JWT token olishda xatolik yuz berdi!");
            }
            StartSocketClient();
        }

        private static async Task SendProgramInfo()
        {
            Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati olinmoqda...");
            var programs = ApplicationMonitor.ApplicationMonitor.GetInstalledPrograms();
            bool success = await ApiClient.SendProgramInfo(await programs);

            if (success)
            {
                SQLiteHelper.WriteLog("Modules", "SendProgramInfo", "Dasturlar ro‘yxati API ga muvaffaqiyatli jo‘natildi");
                Console.WriteLine("[Application Monitor] Dasturlar ro‘yxati muvaffaqiyatli jo‘natildi.");
                SQLiteHelper.UpdateLastSentTime(DateTime.UtcNow);
            }
            else
            {
                SQLiteHelper.WriteLog("Modules", "SendProgramInfo", "Dasturlar ro‘yxatini jo‘natishda xatolik yuz berdi");
                Console.WriteLine("[Application Monitor] Dasturlar ro‘yxatini jo‘natishda xatolik yuz berdi!");
            }
        }

        private static void StartTimer()
        {
            SQLiteHelper.WriteLog("Modules", "StartTimer", "24 soatlik timer ishga tushirildi");
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(24)
            };

            timer.Tick += async (sender, args) =>
            {
                Console.WriteLine("[Timer] 24 soat o‘tdi, yangi ma’lumot yuborilmoqda...");
                SQLiteHelper.WriteLog("Modules", "StartTimer", "24 soat o‘tdi, yangi ma’lumot yuborilmoqda");

                if (!SQLiteHelper.ShouldSendProgramInfo())
                {
                    SQLiteHelper.WriteLog("Modules", "StartTimer", "24 soat o‘tmaganligi sababli dasturlar ro‘yxati jo‘natilmadi");
                    Console.WriteLine("[Timer] 24 soat o‘tmaganligi sababli dasturlar ro‘yxati jo‘natilmadi.");
                    return;
                }

                await SendProgramInfo();
            };

            timer.Start();
        }

        public static void Exe_info_CreateJson()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string sourceDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\TestApp\bin\Release"));
            string exePath = Path.Combine(sourceDir, "DgzAIO.exe");
            string jsonPath = Path.Combine(sourceDir, "agent_update_info.json");


            string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath).FileVersion;
            var updateInfo = new
            {
                version = version,
            };

            string json = JsonConvert.SerializeObject(updateInfo, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
            Console.WriteLine("Update info json tayyorlandi!");

        }
    }
}



