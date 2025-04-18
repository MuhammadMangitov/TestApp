using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgzAIO
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AgentUpdater agentUpdater = new AgentUpdater();
                agentUpdater.CheckAndUpdate().Wait();

                Modules.Start();
                Console.WriteLine("Barcha modullar ishga tushdi!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xatolik yuz berdi: {ex}");
            }

            Console.ReadLine();
        }
        
    }

}

