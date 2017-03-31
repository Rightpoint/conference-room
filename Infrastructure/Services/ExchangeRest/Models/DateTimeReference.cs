using System;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    public class DateTimeReference
    {
        public string DateTime { get; set; }
        public string TimeZone { get; set; }

        public DateTimeOffset ToOffset()
        {
            if (TimeZone != "UTC")
            {
                throw new ArgumentException($"Invalid timezone: {TimeZone}");
            }
            return DateTimeOffset.Parse(DateTime + " +0:00");
        }
    }
}