using System;
using System.Collections.Concurrent;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class NamedConcurrencyLimiter : INamedConcurrencyLimiter
    {
        private readonly int _limit;
        public NamedConcurrencyLimiter(int limit)
        {
            _limit = limit;
        }

        private ConcurrentDictionary<string, IConcurrencyLimiter> _limiters = new ConcurrentDictionary<string, IConcurrencyLimiter>();

        public IDisposable StartOperation(string name)
        {
            return _limiters.GetOrAdd(name, _ => new ConcurrencyLimiter(_limit)).StartOperation();
        }
    }
}