using System;
using System.Collections.Generic;
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

