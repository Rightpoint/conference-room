using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IConferenceRoomService
    {
        IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(string roomAddress);

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        IEnumerable<RoomList> GetRoomLists();

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetRoomLists()"/></param>
        /// <returns></returns>
        IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress);
    }
}
