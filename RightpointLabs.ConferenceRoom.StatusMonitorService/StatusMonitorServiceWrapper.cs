using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    public partial class StatusMonitorServiceWrapper : ServiceBase
    {
        StatusMonitor _statusMonitor = new StatusMonitor();
        public StatusMonitorServiceWrapper()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _statusMonitor.Start();
        }

        protected override void OnStop()
        {
            _statusMonitor.Stop();
        }
    }
}
