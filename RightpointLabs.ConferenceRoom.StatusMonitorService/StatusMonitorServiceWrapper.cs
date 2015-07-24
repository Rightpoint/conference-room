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
        private StatusMonitor _statusMonitor;
        public StatusMonitorServiceWrapper()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _statusMonitor = new StatusMonitor();
            _statusMonitor.Start();
        }

        protected override void OnStop()
        {
            _statusMonitor.Stop();
            _statusMonitor = null;
        }
    }
}
