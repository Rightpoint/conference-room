namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class NotificationRequest
    {
        public int ConnectionTimeoutInMinutes { get; set; }
        public int KeepAliveNotificationIntervalInSeconds { get; set; }
        public string[] SubscriptionIds { get; set; }
    }
}