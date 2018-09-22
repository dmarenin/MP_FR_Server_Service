using System;
using System.Collections.Generic;

using System.ServiceProcess;
using System.Text;


namespace MP_FR_Server_Service
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MP_FR_Server_Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
