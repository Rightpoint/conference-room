namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class Attendee : Recipient
    {
        public AttendeeStatus Status { get; set; }
        public string Type { get; set; }
    }
}