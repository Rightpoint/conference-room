using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class CalendarEntry
    {
        public Attendee[] Attendees { get; set; }
        public string ChangeKey { get; set; }
        public DateTimeReference End { get; set; }
        public Importance Importance { get; set; }
        public bool IsAllDay { get; set; }
        public DateTimeReference Start { get; set; }
        public string OnlineMeetingUrl { get; set; }
        public Attendee Organizer { get; set; }
        public Sensitivity Sensitivity { get; set; }
        public string Subject { get; set; }
        public ShowAs ShowAs { get; set; }
        public string Id { get; set; }
        public Item Location { get; set; }
        public BodyContent Body { get; set; }
    }
}