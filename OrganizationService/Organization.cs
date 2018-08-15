using System.ComponentModel;
using RightpointLabs.ConferenceRoom.Shared;

namespace OrganizationService
{
    public class Organization : Entity
    {
        public string JoinKey { get; set; }
        public string[] UserDomains { get; set; }
        public string[] Administrators { get; set; }
        public string TimeZoneId { get; set; }
        [DisplayName("Warn after x seconds if meeting not started (0 to disable)")]
        public int WarnNonStartedMeetingDelay { get; set; } = 300;
        [DisplayName("After warning, cancel after x seconds if meeting not started (0 to disable)")]
        public int AutoCancelNonStartedMeetingDelay { get; set; } = 120;
    }
}
