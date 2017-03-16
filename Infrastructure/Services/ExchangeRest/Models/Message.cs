namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class Message
    {
        public string Subject { get; set; }
        public BodyContent Body { get; set; }
        public Recipient[] ToRecipients { get; set; }
        public Recipient From { get; set; }
        public Recipient[] ReplyTo { get; set; }
    }
}