using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Models
{
    public class ExchangeConferenceRoomServiceConfiguration
    {
        public bool IgnoreFree { get; set; }
        public bool UseChangeNotification { get; set; }
        public bool ImpersonateForAllCalls { get; set; }
        public string[] EmailDomains { get; set; }
    }
}
