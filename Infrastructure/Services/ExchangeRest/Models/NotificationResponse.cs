using System;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class NotificationResponse
    {
        public string SubscriptionId { get; set; }
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }
        public int SequenceNumber { get; set; }
        public string ChangeType { get; set; }
        public string Resource { get; set; }
        public JObject ResourceData { get; set; }
    }
}