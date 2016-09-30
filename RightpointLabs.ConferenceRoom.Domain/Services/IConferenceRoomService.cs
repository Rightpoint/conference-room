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
        void StartMeeting(string roomAddress, string uniqueId);
        bool StartMeetingFromClient(string roomAddress, string uniqueId, string signature);
        void WarnMeeting(string roomAddress, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl);
        void CancelMeeting(string roomAddress, string uniqueId);
        void EndMeeting(string roomAddress, string uniqueId);
        void StartNewMeeting(string roomAddress, string title, DateTime endTime);
        RoomInfo GetInfo(string roomAddress = null);
        void MessageMeeting(string roomAddress, string uniqueId);
        bool CancelMeetingFromClient(string roomAddress, string uniqueId, string signature);
        void SecurityCheck(string roomAddress);
    }
}
