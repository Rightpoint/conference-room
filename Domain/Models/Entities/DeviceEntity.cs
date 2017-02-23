using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class DeviceEntity : Entity, IByOrganizationId
    {
        public string OrganizationId { get; set; }
        public string BuildingId { get; set; }
        public string[] ControlledRoomIds { get; set; }
        public DeviceState ReportedState { get; set; }
        [DisplayName("Warn after x seconds if meeting not started (0 to disable)")]
        public int? WarnNonStartedMeetingDelay { get; set; }
        [DisplayName("After warning, cancel after x seconds if meeting not started (0 to disable)")]
        public int? AutoCancelNonStartedMeetingDelay { get; set; }

        public class DeviceState
        {
            public string Hostname { get; set; }
            public string[] Addresses { get; set; }
            public int ReportedTimezoneOffset { get; set; }
            public DateTime ReportedUtcTime { get; set; }
            public DateTime ServerUtcTime { get; set; }
        }
    }
}
