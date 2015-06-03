using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class MeetingCacheService : IMeetingCacheService
    {
        private ConcurrentDictionary<string, Task<IEnumerable<Meeting>>> _tasks = new ConcurrentDictionary<string, Task<IEnumerable<Meeting>>>();

        public Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(string roomAddress, Func<Task<IEnumerable<Meeting>>> loader)
        {
            return _tasks.GetOrAdd(roomAddress, (a) => loader());
        }

        public void ClearUpcomingAppointmentsForRoom(string roomAddress)
        {
            Task<IEnumerable<Meeting>> value;
            _tasks.TryRemove(roomAddress, out value);
            // we don't care if there was an item to remove or not
        }
    }
}
