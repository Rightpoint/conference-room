using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class MeetingCacheService : IMeetingCacheService
    {
        private class ValueHolder
        {
            public ValueHolder(DateTime created, Task<IEnumerable<Meeting>> value)
            {
                Created = created;
                Value = value;
            }

            public DateTime Created { get; set; }
            public Task<IEnumerable<Meeting>> Value { get; set; }
        }

        private ConcurrentDictionary<string, ValueHolder> _tasks = new ConcurrentDictionary<string, ValueHolder>();
        private ConcurrentDictionary<string, IMeetingCacheReloader> _cacheReloaders = new ConcurrentDictionary<string, IMeetingCacheReloader>();

        public void ConfigureReloader(string roomAddress, IMeetingCacheReloader reloader)
        {
            _cacheReloaders.AddOrUpdate(roomAddress, reloader, (k,v) => reloader);
        }

        public Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(string roomAddress, bool isTracked, Func<Task<IEnumerable<Meeting>>> loader)
        {
            var now = DateTime.UtcNow;
            var cached = _tasks.GetOrAdd(roomAddress, (a) => new ValueHolder(now, loader()));

            // let's make sure it's not too stale....
            var allowedDelay = isTracked ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(15);
            lock (cached)
            {
                if (cached.Created.Add(allowedDelay) < now)
                {
                    // too old
                    cached.Created = now;
                    cached.Value = loader();
                }
                return cached.Value;
            }
        }
        public Task<IEnumerable<Meeting>> TryGetUpcomingAppointmentsForRoom(string roomAddress, bool isTracked)
        {
            var now = DateTime.UtcNow;
            ValueHolder cached;
            _tasks.TryGetValue(roomAddress, out cached);
            if (null == cached)
                return null;

            // let's make sure it's not too stale....
            var allowedDelay = isTracked ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(15);
            if (cached.Created.Add(allowedDelay) < now)
            {
                // too old
                return null;
            }
            return cached.Value;
        }

        public void ClearUpcomingAppointmentsForRoom(string roomAddress)
        {
            ValueHolder value;
            _tasks.TryRemove(roomAddress, out value);
            // we don't care if there was an item to remove or not

            // however, if we have a reloader, we should use it to reload our cache
            if (_cacheReloaders.TryGetValue(roomAddress, out IMeetingCacheReloader reloader))
            {
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    new TelemetryClient().TrackTrace($"Reloading cache for {roomAddress}", SeverityLevel.Verbose);
                    await reloader.ReloadCache(roomAddress);
                });
            }
        }
    }
}
