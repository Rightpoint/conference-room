using System;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Services.Models
{
    public class Meeting
    {
        public ItemId Id { get; set; }
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Organizer { get; set; }
        public bool IsStarted { get; set; }
        public bool IsEndedEarly { get; set; }
        public bool IsCancelled { get; set; }
    }
}