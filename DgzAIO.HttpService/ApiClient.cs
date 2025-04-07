using ComputerInformation;
using DBHelper;
using DgzAIO.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DgzAIO.HttpService
{
    public class ApiClient
    {
        private static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        /*
        private static readonly string BaseUrl = ConfigurationManager.GetBaseUrl();
        private static readonly string BaseUrlForApps = ConfigurationManager.GetBaseUrlForApps();*/

        /*private static readonly string BaseUrl = "http://16.171.135.170:4000/v1/agent/create";
        private static readonly string BaseUrlForApps = "http://16.171.135.170:4000/v1/agent/applications";*/

        private static readonly string BaseUrl = "http://16.171.135.170:4000/v1/agent/create";
        private static readonly string BaseUrlForApps = "http://16.171.135.170:4000/v1/agent/applications";
        public static async Task<(string token, int statusCode)> GetJwtTokenFromApi()
        {
            try
            {
                var computerInfo = await ComputerInfo.GetComputerInfoAsync();
                Console.WriteLine($"Kompyuter ma'lumotlari: {JsonConvert.SerializeObject(computerInfo)}");

                var jsonContent = JsonConvert.SerializeObject(computerInfo);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(BaseUrl, content);
                int statusCode = (int)response.StatusCode;
                Console.WriteLine($"RESPONSE URL: {response.RequestMessage}");
                Console.WriteLine($"RESPONSE : {response}");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    string token = jsonResponse?.token;

                    return (token, statusCode);
                }
                else
                {
                    Console.WriteLine($"JWT olishda xatolik: {response.StatusCode}");
                    return (null, statusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API ga so'rov yuborishda xatolik: {ex.Message}");
                SQLiteHelper.WriteError($"API ga so'rov yuborishda xatolik: {ex.Message}");
                return (null, 500);
            }
        }

        private static async Task<bool> SendData<T>(string url, T data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await DBHelper.SQLiteHelper.GetJwtToken();
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new
                        System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                Console.WriteLine($"[Xatolik]: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[HTTP Xatolik]: {httpEx.Message}");
                SQLiteHelper.WriteError($"HTTP Xatolik: {httpEx.Message}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[HTTP Xatolik]: So‘rov vaqt chegarasidan oshib ketdi.");
                SQLiteHelper.WriteError("HTTP Xatolik: So‘rov vaqt chegarasidan oshib ketdi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Noma'lum xatolik]: {ex.Message}");
                SQLiteHelper.WriteError($"Noma'lum xatolik: {ex.Message}");
            }

            return false;
        }

        public static async Task<bool> SendProgramInfo(List<ProgramDetails> programs)
        {
            return await SendData(BaseUrlForApps, programs);
        }

        public static async Task<bool> SendCommandResult(string command, string result, string error)
        {
            var response = new { command, result, error };
            return await SendData($"{BaseUrl}/command-result", response);
        }

    }
}
