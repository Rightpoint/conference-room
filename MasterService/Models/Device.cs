using System;
using System.ComponentModel;
using RightpointLabs.ConferenceRoom.Shared;

namespace MasterService.Models
{
    public class Device : Entity, IByOrganizationId
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
            public DateTimeOffset ReportedUtcTime { get; set; }
            public DateTimeOffset ServerUtcTime { get; set; }
        }
    }
}
