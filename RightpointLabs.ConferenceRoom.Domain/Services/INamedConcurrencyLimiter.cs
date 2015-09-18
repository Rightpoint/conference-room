using System;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface INamedConcurrencyLimiter
    {
        IDisposable StartOperation(string name);
    }
}