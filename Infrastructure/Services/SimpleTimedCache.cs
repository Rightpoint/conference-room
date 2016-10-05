using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class SimpleTimedCache : ISimpleTimedCache
    {
        private class ValueHolder
        {
            public ValueHolder(DateTime created, Task value)
            {
                Created = created;
                Value = value;
            }

            public DateTime Created { get; set; }
            public Task Value { get; set; }
        }

        private ConcurrentDictionary<string, ValueHolder> _tasks = new ConcurrentDictionary<string, ValueHolder>();

        public Task<T> GetCachedValue<T>(string key, TimeSpan ageLimit, Func<Task<T>> loader)
        {
            var now = DateTime.UtcNow;
            var cached = _tasks.GetOrAdd(key, (a) => new ValueHolder(now, loader()));

            // let's make sure it's not too stale....
            lock (cached)
            {
                if (cached.Created.Add(ageLimit) < now)
                {
                    // too old
                    cached.Created = now;
                    cached.Value = loader();
                }
                return (Task<T>)cached.Value;
            }
        }

    }
}
