namespace RightpointLabs.ConferenceRoom.Services.Models
{
    public class MeetingInfo
    {
        public string UniqueId { get; set; }
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
    }
}