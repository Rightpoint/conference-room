using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class Meeting
    {
        public string UniqueId { get; set; }
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Organizer { get; set; }
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsNotManaged { get; set; }
        public int RequiredAttendees { get; set; }
        public int OptionalAttendees { get; set; }
        public int ExternalAttendees { get; set; }
    }
}