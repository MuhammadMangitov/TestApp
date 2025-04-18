using System.Data.SQLite;
using SQLitePCL;
using System;
using System.IO;
using DgzAIO.HttpService;
using System.Threading.Tasks;
using System.Linq;

namespace DBHelper
{
    public class SQLiteHelper
    {
        public static string ApplicationDirectory => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                AppDomain.CurrentDomain.FriendlyName);

        public static SQLiteConnection CreateConnection()
        {
            string appDirectory = ApplicationDirectory.Split('.').FirstOrDefault(); ;

            try
            {
                if (!Directory.Exists(appDirectory))
                {
                    Directory.CreateDirectory(appDirectory);
                    Console.WriteLine($"Katalog yaratildi: {appDirectory}");
                }

                string dbPath = Path.Combine(appDirectory, "DgzAIO.db");

                if (!File.Exists(dbPath))
                {
                    Console.WriteLine("Baza mavjud emas! Fayl yaratilmoqda...");
                    SQLiteConnection.CreateFile(dbPath);  
                }

                SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xatolik: {ex.Message}");
                throw;
            }
        }

        public static void CreateTablesIfNotExists()
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS LogEntry (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            module TEXT NOT NULL,
                            function TEXT NOT NULL,
                            created_date TEXT NOT NULL,
                            message TEXT NOT NULL
                        )";
                        command.ExecuteNonQuery();

                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Error (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            message TEXT NOT NULL,
                            created_date TEXT NOT NULL
                        )";
                        command.ExecuteNonQuery();

                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Configurations (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            server_ip TEXT NOT NULL,
                            Jwt_token TEXT NOT NULL,
                            report_time INTEGER,
                            modules TEXT NOT NULL,
                            last_sent_time TEXT
                        )";
                        command.ExecuteNonQuery();

                        command.CommandText = @"
                        INSERT OR IGNORE INTO Configurations (id, server_ip, Jwt_token, report_time, modules, last_sent_time) 
                        VALUES (1, 'default_ip', 'default_token', 0, 'default_modules', NULL)";
                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine("Jadvallar muvaffaqiyatli yaratildi yoki allaqachon mavjud.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Jadvallarni yaratishda xatolik: {ex.Message}");
                }
            }
        }

        public static void InsertJwtToken(string token)
        {
            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE Configurations SET Jwt_token = @jwt_token WHERE id = 1";
                        command.Parameters.AddWithValue("@jwt_token", token);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                }
            }
        }

        public static void DeleteOldJwtToken()
        {
            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE Configurations SET Jwt_token = NULL WHERE Jwt_token IS NOT NULL";
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                }
            }
        }

        public static async Task<string> GetJwtToken()
        {
            string token = null;

            using (var connection = CreateConnection())
            {
                if (connection == null) return null;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Jwt_token FROM Configurations LIMIT 1";

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                token = reader["Jwt_token"]?.ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                }
            }

            return token;
        }

        public static void ClearLogs()
        {
            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.CommandText = "DELETE FROM LogEntry";
                        deleteCommand.ExecuteNonQuery();
                    }

                    using (var resetCommand = connection.CreateCommand())
                    {
                        resetCommand.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'LogEntry'";
                        resetCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("Loglar muvaffaqiyatli o‘chirildi va ID 1 dan boshlashga sozlandi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Loglarni o‘chirishda xato: {ex.Message}");
                }
            }
        }

        public static void WriteLog(string module, string function, string message)
        {
            if (string.IsNullOrEmpty(module) || string.IsNullOrEmpty(function) || string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Module, function yoki message bo‘sh bo‘lmasligi kerak");
                return;
            }

            var createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO LogEntry (module, function, created_date, message) 
                            VALUES (@module, @function, @created_date, @message)";
                        command.Parameters.AddWithValue("@module", module);
                        command.Parameters.AddWithValue("@function", function);
                        command.Parameters.AddWithValue("@created_date", createdDate);
                        command.Parameters.AddWithValue("@message", message);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Log yozishda xato: {ex.Message}");
                }
            }
        }

        public static void WriteError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Xato xabari bo‘sh bo‘lishi mumkin emas.");
                return;
            }

            var createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var command = new SQLiteCommand(@"INSERT INTO ""Error"" (""message"", ""created_date"") 
                                                     VALUES (@message, @created_date)", connection))
                    {
                        command.Parameters.AddWithValue("@message", message);
                        command.Parameters.AddWithValue("@created_date", createdDate);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatoni yozishda xatolik yuz berdi: {ex.Message}");
                }
            }
        }

        public static DateTime? GetLastSentTime()
        {
            using (var connection = CreateConnection())
            {
                if (connection == null) return null;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT last_sent_time FROM Configurations LIMIT 1";

                        var result = command.ExecuteScalar();
                        if (result != null && DateTime.TryParse(result.ToString(), out DateTime lastSentTime))
                        {
                            return lastSentTime;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                }
            }
            return null;
        }

        public static void UpdateLastSentTime(DateTime dateTime)
        {
            using (var connection = CreateConnection())
            {
                if (connection == null) return;

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE Configurations SET last_sent_time = @last_sent_time WHERE id = 1";
                        command.Parameters.AddWithValue("@last_sent_time", dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xatolik: {ex.Message}");
                }
            }
        }
        public static bool ShouldSendProgramInfo()
        {
            DateTime? lastSentTime = GetLastSentTime();
            if (lastSentTime == null) return true;

            return (DateTime.UtcNow - lastSentTime.Value).TotalHours >= 24;
        }
    }
}
