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
        Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(string roomAddress, Func<Task<IEnumerable<Meeting>>> loader);
        void ClearUpcomingAppointmentsForRoom(string roomAddress);
        void ClearAll();
    }
}
