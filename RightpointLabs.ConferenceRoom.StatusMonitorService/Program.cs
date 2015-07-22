using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            InitLogging();
            if (args.Length > 0 && args[0] == "RUN")
            {
                new StatusMonitor().Start();
                while (true)
                {
                    Thread.Sleep(-1);
                }
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] 
                { 
                    new StatusMonitorServiceWrapper() 
                });
            }
        }

        private static void InitLogging()
        {
            // initialize log4net
            var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            if (System.IO.File.Exists(file))
            {
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(file));
            }
        }
    }
}
