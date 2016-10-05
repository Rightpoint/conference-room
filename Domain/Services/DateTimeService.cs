using System;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeSpan _delta;

        public DateTimeService(TimeSpan delta)
        {
            _delta = delta;
        }

        public DateTime Now
        {
            get
            {
                return DateTime.Now.Add(_delta);
            }
        }

        public DateTime UtcNow
        {
            get
            {
            return DateTime.UtcNow.Add(_delta);
            }
        }
    }
}
