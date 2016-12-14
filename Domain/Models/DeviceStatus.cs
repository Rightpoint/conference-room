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
        public decimal? Temperature1 { get; set; }
        public decimal? Temperature2 { get; set; }
        public decimal? Temperature3 { get; set; }
        public decimal? Voltage1 { get; set; }
        public decimal? Voltage2 { get; set; }
        public decimal? Voltage3 { get; set; }
    }
}
