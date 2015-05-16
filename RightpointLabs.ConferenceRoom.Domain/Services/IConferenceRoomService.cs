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

        RoomStatusInfo GetStatus(string roomAddress);
        void StartMeeting(string roomAddress, string uniqueId, string securityKey);
        void WarnMeeting(string roomAddress, string uniqueId, string securityKey);
        void CancelMeeting(string roomAddress, string uniqueId, string securityKey);
        void EndMeeting(string roomAddress, string uniqueId, string securityKey);
        void StartNewMeeting(string roomAddress, string securityKey, string title, int minutes);
        object GetInfo(string roomAddress, string securityKey = null);
        void RequestAccess(string roomAddress, string securityKey, string clientInfo);
    }
}
