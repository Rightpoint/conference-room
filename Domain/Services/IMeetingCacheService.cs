using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IMeetingCacheService
    {
        Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(string roomAddress, bool isTracked, Func<Task<IEnumerable<Meeting>>> loader);
        void ClearUpcomingAppointmentsForRoom(string roomAddress);
        Task<IEnumerable<Meeting>> TryGetUpcomingAppointmentsForRoom(string roomAddress, bool isTracked);
        void ConfigureReloader(string roomAddress, IMeetingCacheReloader reloader);
    }
}
