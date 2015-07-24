using System;
using System.Threading;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ConcurrencyLimiter : IConcurrencyLimiter
    {
        private readonly object _lock = new object();
        private readonly int _limit;
        private int _remaining;
        public ConcurrencyLimiter(int limit)
        {
            _limit = limit;
            _remaining = _limit;
        }

        public IDisposable StartOperation()
        {
            lock (_lock)
            {
                while (_remaining <= 0)
                {
                    Monitor.Wait(_lock);
                }
                _remaining--;
            }
            return new ReleaseDisposable(this);
        }

        private class ReleaseDisposable : IDisposable
        {
            private readonly ConcurrencyLimiter _parent;

            public ReleaseDisposable(ConcurrencyLimiter parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                lock (_parent._lock)
                {
                    _parent._remaining++;
                    Monitor.Pulse(_parent._lock);
                }
            }
        }
    }
}
