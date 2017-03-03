using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IConferenceRoomService
    {
        Task<RoomInfo> GetStaticInfo(IRoom room);
        Task<RoomStatusInfo> GetStatus(IRoom room, bool isSimple = false);
        Task StartMeeting(IRoom room, string uniqueId);
        Task<bool> StartMeetingFromClient(IRoom room, string uniqueId, string signature);
        Task WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl);
        Task AbandonMeeting(IRoom room, string uniqueId);
        Task CancelMeeting(IRoom room, string uniqueId);
        Task EndMeeting(IRoom room, string uniqueId);
        Task StartNewMeeting(IRoom room, string title, DateTime endTime);
        Task MessageMeeting(IRoom room, string uniqueId);
        Task<bool> CancelMeetingFromClient(IRoom room, string uniqueId, string signature);
        Task SecurityCheck(IRoom room);
        Task<Dictionary<string, Tuple<RoomInfo, IRoom>>> GetInfoForRoomsInBuilding(string buildingId);
    }

}
