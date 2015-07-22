using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.StatusMonitorService
{
    public class StatusMonitor
    {
        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        private void GetStatus()
        {
            var c = new HttpClient();
            var uri = new Uri(new Uri(Config.ApiServer), "room/" + Config.RoomAddress + "/status");
            var data = c.GetAsync(uri).Result;
        }
    }
}
