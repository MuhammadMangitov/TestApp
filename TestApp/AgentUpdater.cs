using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.ServiceModel;
using System.Diagnostics;
using System.Threading.Tasks;
using DgzAIO.Models;
using DgzAIOWindowsService;
using DgzAIO;

public class AgentUpdater
{
    private static readonly string SERVER_URL = ConfigurationManagerDgzAIO.DgzApi.SERVER_URL_UPDATE_AGENT;
    private static readonly string DOWNLOAD_URL = ConfigurationManagerDgzAIO.DgzApi.DOWNLOAD_URL_AGENT_ZIP;

    private const string TOKEN_FOR_UPDATE = "Assalomu alaykum. DGZ server updateni tekshirishga keldim!!!";
    private static readonly HttpClient client = new HttpClient();
    private readonly string currentVersion;
    private readonly string localPath;

    public AgentUpdater()
    {
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        currentVersion = FileVersionInfo.GetVersionInfo(exePath).FileVersion;
        localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Agent");
        if (!Directory.Exists(localPath))
        {
            try
            {
                Directory.CreateDirectory(localPath);
            }
            catch (UnauthorizedAccessException)
            {
                Log("C:\\ProgramData\\Agent\\ papkasini yaratish uchun administrator huquqlari kerak!");
                Console.WriteLine("C:\\ProgramData\\Agent\\ papkasini yaratish uchun administrator huquqlari kerak!");
                throw;
            }
        }
        client.DefaultRequestHeaders.Add("Authorization", TOKEN_FOR_UPDATE);
    }

    [ServiceContract]
    interface IAgentService
    {
        [OperationContract]
        void UpdateAgent(string zipPath);
    }

    public async Task CheckAndUpdate()
    {
        try
        {
            Console.WriteLine($" aaaa{ localPath}");

            Console.WriteLine("Yangilanish tekshirilmoqda...");
            Log("Yangilanish tekshirilmoqda...");
            string jsonResponse = await client.GetStringAsync(SERVER_URL);
            Log("Server javobi: " + jsonResponse);
            Console.WriteLine("Server javobi: " + jsonResponse);
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(jsonResponse);
            if (updateInfo == null)
            {
                Log("Deserialization muvaffaqiyatsiz bo‘ldi!");
                Console.WriteLine("Deserialization muvaffaqiyatsiz bo‘ldi!");
                return;
            }

            Log("Deserialization muvaffaqiyatli bo‘ldi!");
            Console.WriteLine("Deserialization muvaffaqiyatli bo‘ldi!");
            Log("Server versiyasi: " + updateInfo.Version);
            Log("Joriy versiya: " + currentVersion);
            Console.WriteLine("Server versiyasi: " + updateInfo.Version);
            Console.WriteLine("Joriy versiya: " + currentVersion);

            if (IsNewerVersion(updateInfo.Version, currentVersion))
            {
                string zipFileName = updateInfo.FileName;
                string downloadUrl = DOWNLOAD_URL + zipFileName;
                string zipPath = Path.Combine(localPath, zipFileName);

                Log($"ZIP fayli yuklanmoqda: {downloadUrl}");
                Console.WriteLine($"ZIP fayli yuklanmoqda: {downloadUrl}");
                var zipBytes = await client.GetByteArrayAsync(downloadUrl);
                try
                {
                    File.WriteAllBytes(zipPath, zipBytes);
                    Log($"ZIP fayli saqlandi: {zipPath}");
                }
                catch (IOException ex)
                {
                    Log($"Faylni {zipPath} ga yozishda xato: {ex.Message}");
                    Console.WriteLine($"Faylni {zipPath} ga yozishda xato: {ex.Message}");
                    throw;
                }

                SendUpdateToService(zipPath);
                Log("Yangilanish yuklab olindi va xizmatga yuborildi.");
                Console.WriteLine("Yangilanish yuklab olindi va xizmatga yuborildi.");
            }
            else
            {
                Log("Joriy versiya yangi yoki teng.");
                Console.WriteLine("Joriy versiya yangi yoki teng.");
            }
        }
        catch (HttpRequestException ex)
        {
            Log($"Server bilan aloqa xatosi: {ex.Message}");
            Console.WriteLine($"Server bilan aloqa xatosi: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log($"Xatolik yuz berdi: {ex.Message}");
            Console.WriteLine($"Xatolik yuz berdi: {ex.Message}");
        }
    }

    private bool IsNewerVersion(string serverVersion, string currentVersion)
    {
        try
        {
            Version serverVer = new Version(serverVersion);
            Version currentVer = new Version(currentVersion);
            return serverVer > currentVer;
        }
        catch (Exception ex)
        {
            Log($"Versiyalarni solishtirishda xato: {ex.Message}");
            return false;
        }
    }

    private void SendUpdateToService(string zipPath)
    {
        Log("Yangilanish xizmatga yuborilmoqda...");
        Console.WriteLine("Yangilanish xizmatga yuborilmoqda...");
        var binding = new NetNamedPipeBinding();
        var endpoint = new EndpointAddress("net.pipe://localhost/DgzAIOWindowsService");

        using (var factory = new ChannelFactory<IAgentService>(binding, endpoint))
        {
            var channel = factory.CreateChannel();
            var clientChannel = (IClientChannel)channel;

            bool success = false;

            try
            {
                channel.UpdateAgent(zipPath);
                success = true;
            }
            catch (EndpointNotFoundException)
            {
                Log("❌ Xizmat topilmadi, ishga tushirilganligini tekshiring.");
            }
            catch (CommunicationException ex)
            {
                Log($"❌ WCF aloqa xatosi: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"❌ Xatolik: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (clientChannel.State != CommunicationState.Faulted)
                    {
                        clientChannel.Close();
                    }
                }
                catch
                {
                    clientChannel.Abort();
                }

                if (success)
                {
                    Log("✅ Yangilanish xizmatga muvaffaqiyatli yuborildi.");
                }
            }
        }
    }
    private void Log(string message)
    {
        string logPath = Path.Combine(localPath, "agent_updater.log");
        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now}: {message}\n");
        }
        catch
        {
            Console.WriteLine(message); // Agar log fayliga yozish muvaffaqiyatsiz bo'lsa, konsolga chiqarish
        }
    }
}