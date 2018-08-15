using System;
using RightpointLabs.ConferenceRoom.Shared;

namespace MasterService.Models
{
    public class DeviceStatus : Entity
    {
        public string OrganizationId { get; set; }
        public string DeviceId { get; set; }
        public DateTimeOffset StatusTimestamp { get; set; }
        public double? Temperature1 { get; set; }
        public double? Temperature2 { get; set; }
        public double? Temperature3 { get; set; }
        public double? Voltage1 { get; set; }
        public double? Voltage2 { get; set; }
        public double? Voltage3 { get; set; }
    }
}
