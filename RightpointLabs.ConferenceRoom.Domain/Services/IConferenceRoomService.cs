using System;
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

        RoomStatusInfo GetStatus(string roomAddress, bool isSimple = false);
        void StartMeeting(string roomAddress, string uniqueId, string securityKey);
        bool StartMeetingFromClient(string roomAddress, string uniqueId, string signature);
        void WarnMeeting(string roomAddress, string uniqueId, string securityKey, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl);
        void CancelMeeting(string roomAddress, string uniqueId, string securityKey);
        void EndMeeting(string roomAddress, string uniqueId, string securityKey);
        void StartNewMeeting(string roomAddress, string securityKey, string title, DateTime endTime);
        RoomInfo GetInfo(string roomAddress, string securityKey = null);
        void RequestAccess(string roomAddress, string securityKey, string clientInfo);
        void MessageMeeting(string roomAddress, string uniqueId, string securityKey);
        bool CancelMeetingFromClient(string roomAddress, string uniqueId, string signature);
        void SecurityCheck(string roomAddress, string securityKey);
    }
}
