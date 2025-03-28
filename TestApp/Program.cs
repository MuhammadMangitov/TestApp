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
            Modules.StartDBHelper();
            //Modules.StartApplicationMonitor();
            //Modules.StartSocketClient();
        }
    }
}
