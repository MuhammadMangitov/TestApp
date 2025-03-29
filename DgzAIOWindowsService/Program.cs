using System;
using System.ServiceProcess;

namespace DgzAIOWindowsService
{
    static class Program
    {
        // windows user inpersonation
        // service ga prossec watcher qo'shish kerak.
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
