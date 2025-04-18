using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using System;

namespace DgzAIO
{
    public class PipeClient
    {
        public async Task SendMessageAsync(string message)
        {
            using (var client = new NamedPipeClientStream(".", "DgzPipe", PipeDirection.InOut))
            {
                await client.ConnectAsync();

                using (var reader = new StreamReader(client))
                using (var writer = new StreamWriter(client) { AutoFlush = true })
                {
                    await writer.WriteLineAsync(message);
                    string response = await reader.ReadLineAsync();
                    Console.WriteLine($"Service javobi: {response}");
                }
            }
        }
    }
}
