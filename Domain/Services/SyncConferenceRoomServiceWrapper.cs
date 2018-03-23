using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public class SyncConferenceRoomServiceWrapper : IConferenceRoomService
    {
        private readonly ISyncConferenceRoomService _service;

        public SyncConferenceRoomServiceWrapper(ISyncConferenceRoomService service)
        {
            _service = service;
        }
        
        public Task<RoomInfo> GetStaticInfo(IRoom room)
        {
            return Task.Run(() => _service.GetStaticInfo(room));
        }

        public Task<RoomStatusInfo> GetStatus(IRoom room, bool isSimple = false)
        {
            return Task.Run(() => _service.GetStatus(room, isSimple));
        }

        public Task StartMeeting(IRoom room, string uniqueId)
        {
            return Task.Run(() => _service.StartMeeting(room, uniqueId));
        }

        public Task<bool> StartMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            return Task.Run(() => _service.StartMeetingFromClient(room, uniqueId, signature));
        }

        public Task WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl)
        {
            return Task.Run(() => _service.WarnMeeting(room, uniqueId, buildStartUrl, buildCancelUrl));
        }

        public Task AbandonMeeting(IRoom room, string uniqueId)
        {
            return Task.Run(() => _service.AbandonMeeting(room, uniqueId));
        }

        public Task CancelMeeting(IRoom room, string uniqueId)
        {
            return Task.Run(() => _service.CancelMeeting(room, uniqueId));
        }

        public Task EndMeeting(IRoom room, string uniqueId)
        {
            return Task.Run(() => _service.EndMeeting(room, uniqueId));
        }

        public Task StartNewMeeting(IRoom room, string title, DateTime endTime)
        {
            return Task.Run(() => _service.StartNewMeeting(room, title, endTime));
        }

        public Task MessageMeeting(IRoom room, string uniqueId)
        {
            return Task.Run(() => _service.MessageMeeting(room, uniqueId));
        }

        public Task<bool> CancelMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            return Task.Run(() => _service.CancelMeetingFromClient(room, uniqueId, signature));
        }

        public Task SecurityCheck(IRoom room)
        {
            return Task.Run(() => _service.SecurityCheck(room));
        }

        public Task<Dictionary<string, Tuple<RoomInfo, IRoom>>> GetInfoForRoomsInBuilding(string buildingId)
        {
            return Task.Run(() => _service.GetInfoForRoomsInBuilding(buildingId));
        }

        public Task ScheduleNewMeeting(IRoom room, string title, DateTimeOffset startTime, DateTimeOffset endTime, bool inviteMe)
        {
            throw new NotImplementedException();
        }
    }
}