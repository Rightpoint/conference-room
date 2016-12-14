using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class DeviceStatus
    {
        public string OrganizationId { get; set; }
        public string DeviceId { get; set; }
        public DateTime StatusTimestamp { get; set; }
        public double? Temperature1 { get; set; }
        public double? Temperature2 { get; set; }
        public double? Temperature3 { get; set; }
        public double? Voltage1 { get; set; }
        public double? Voltage2 { get; set; }
        public double? Voltage3 { get; set; }
    }
}
