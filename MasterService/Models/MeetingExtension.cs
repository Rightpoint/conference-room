using RightpointLabs.ConferenceRoom.Shared;

namespace MasterService.Models
{
    public class MeetingExtension : Entity, IByOrganizationId
    {
        public string OrganizationId { get; set; }
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
    }
}