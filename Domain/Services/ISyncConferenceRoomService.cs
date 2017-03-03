using System;
using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface ISyncConferenceRoomService
    {
        RoomInfo GetStaticInfo(IRoom room);
        RoomStatusInfo GetStatus(IRoom room, bool isSimple = false);
        void StartMeeting(IRoom room, string uniqueId);
        bool StartMeetingFromClient(IRoom room, string uniqueId, string signature);
        void WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl);
        void AbandonMeeting(IRoom room, string uniqueId);
        void CancelMeeting(IRoom room, string uniqueId);
        void EndMeeting(IRoom room, string uniqueId);
        void StartNewMeeting(IRoom room, string title, DateTime endTime);
        void MessageMeeting(IRoom room, string uniqueId);
        bool CancelMeetingFromClient(IRoom room, string uniqueId, string signature);
        void SecurityCheck(IRoom room);
        Dictionary<string, Tuple<RoomInfo, IRoom>> GetInfoForRoomsInBuilding(string buildingId);
    }
}