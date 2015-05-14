namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class MeetingInfo : Entity
    {
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
    }
}