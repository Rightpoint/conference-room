namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class MeetingEntity : Entity
    {
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
    }
}