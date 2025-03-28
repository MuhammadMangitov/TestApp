using System.Data.SQLite;
using SQLitePCL;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DBHelper
{
    public class SQLiteHelper
    {
        private static readonly string dbPath = @"C:\Users\Muhammad\Desktop\c#_modul\github\SystemMonitor\SystemMonitor.db";

        public static SQLiteConnection CreateConnection()
        { 
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("Baza mavjud emas!");


                
                Console.WriteLine($"Fayl mavjudmi? {File.Exists(dbPath)}");

                Console.WriteLine($"Fayl yo'li: {dbPath}");

                return null;
            }

            var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            connection.Open();
            return connection;
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
