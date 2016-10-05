using System;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IDateTimeService
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
}
