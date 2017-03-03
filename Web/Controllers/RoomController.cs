using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using log4net;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConferenceRoomService _conferenceRoomService;
        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IGdoService _gdoService;
        private readonly IContextService _contextService;
        private readonly IBuildingRepository _buildingRepository;

        public RoomController(IConferenceRoomService conferenceRoomService, IRoomMetadataRepository roomRepository, IGdoService gdoService, IContextService contextService, IBuildingRepository buildingRepository)
            : base(__log)
        {
            _conferenceRoomService = conferenceRoomService;
            _roomRepository = roomRepository;
            _gdoService = gdoService;
            _contextService = contextService;
            _buildingRepository = buildingRepository;
        }


        [Route("all")]
        public async Task<object> GetAll()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the info for a single room.
        /// </summary>
        /// <param name="roomId">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomId}/info")]
        public async Task<object> GetInfo(string roomId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            var data = await _conferenceRoomService.GetStaticInfo(room);
            return data;
        }

        /// <summary>
        /// Gets the status for a single room.
        /// </summary>
        /// <param name="roomId">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomId}/status")]
        public async Task<object> GetStatus(string roomId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            var data = await _conferenceRoomService.GetStatus(room);

            var retVal = JObject.FromObject(data);
            var warnDelay = _contextService.CurrentDevice?.WarnNonStartedMeetingDelay ?? _contextService.CurrentOrganization?.WarnNonStartedMeetingDelay ?? 5 * 60;
            var cancelDelay = _contextService.CurrentDevice?.AutoCancelNonStartedMeetingDelay ?? _contextService.CurrentOrganization?.AutoCancelNonStartedMeetingDelay ?? 7 * 60;
            retVal["warnDelay"] = warnDelay;
            retVal["cancelDelay"] = cancelDelay;

            return retVal;
        }

        private async Task AssertRoomIsFromOrg(RoomMetadataEntity room)
        {
            if (null == room)
            {
                throw new ArgumentException();
            }
            if (room.OrganizationId != _contextService.CurrentOrganization.Id)
            {
                throw new AccessDeniedException("Access Denied", null);
            }
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/start")]
        public async Task PostStartMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.StartMeeting(room, uniqueId);
        }

        /// <summary>
        /// Warn attendees this meeting will be marked as abandoned (not started in time) very soon.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/warnAbandon")]
        public async Task PostWarnAbandonMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.WarnMeeting(room, uniqueId,
                signature => new Uri(Request.RequestUri, Url.Route("StartFromClient", new { roomId, uniqueId, signature })).AbsoluteUri,
                signature => new Uri(Request.RequestUri, Url.Route("CancelFromClient", new { roomId, uniqueId, signature })).AbsoluteUri);
        }

        /// <summary>
        /// Marks a meeting as cancelled (user took action).
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/cancel")]
        public async Task PostCancelMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.CancelMeeting(room, uniqueId);
        }

        /// <summary>
        /// Marks a meeting as abandoned (not started in time).
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/abandon")]
        public async Task PostAbandonMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.AbandonMeeting(room, uniqueId);
        }

        /// <summary>
        /// Start a new meeting
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="title">The title of the meeting</param>
        /// <param name="endTime">The time the meeting will end</param>
        [Route("{roomId}/meeting/startNew")]
        public async Task PostStartNewMeeting(string roomId, string title, DateTime endTime)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.StartNewMeeting(room, title, endTime);
        }

        /// <summary>
        /// Marks a meeting as ended early.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/end")]
        public async Task PostEndMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.EndMeeting(room, uniqueId);
        }

        [Route("{roomId}/meeting/message")]
        public async Task PostMessageMeeting(string roomId, string uniqueId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.MessageMeeting(room, uniqueId);
        }

        [Route("all/{buildingId}")]
        public async Task<object> GetAll(string buildingId)
        {
            var building = _buildingRepository.Get(buildingId ?? _contextService.CurrentDevice?.BuildingId);
            if (null == building || building.OrganizationId != _contextService.CurrentOrganization?.Id)
            {
                return NotFound();
            }

            var roomInfos = await _conferenceRoomService.GetInfoForRoomsInBuilding(buildingId);

            // ok, we have the filtered rooms list, now we need to get the status and smash it together with the room data
            return
                roomInfos
                    .Select(i => new { Address = i.Key, i.Value.Item2.Id, Info = i.Value.Item1 })
                    .ToList();
        }

        [Route("all/status/{buildingId}")]
        public async Task<object> GetAllStatus(string buildingId)
        {
            var building = _buildingRepository.Get(buildingId ?? _contextService.CurrentDevice?.BuildingId);
            if (null == building || building.OrganizationId != _contextService.CurrentOrganization?.Id)
            {
                return NotFound();
            }

            var roomInfos = await _conferenceRoomService.GetInfoForRoomsInBuilding(buildingId);
            
            // ok, we have the filtered rooms list, now we need to get the status and smash it together with the room data
            return
                roomInfos.AsParallel()
                    .WithDegreeOfParallelism(256)
                    .Select(i =>
                    {
                        //__log.DebugFormat("Starting {0}", i.Address);
                        var status = _conferenceRoomService.GetStatus(i.Value.Item2, true);
                        //__log.DebugFormat("Got {0}", i.Address);
                        return new {Address = i.Key, i.Value.Item2.Id, Info = i.Value.Item1, Status = status};
                    })
                    .ToList();
        }

        /// <summary>
        /// Open a GDO
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        [Route("{roomId}/door/open")]
        public async Task PostOpenDoor(string roomId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.SecurityCheck(room);
            if (string.IsNullOrEmpty(room.GdoDeviceId))
            {
                throw new ArgumentException("No door to control");
            }
            await _gdoService.Open(room.GdoDeviceId);
        }

        /// <summary>
        /// Close a GDO
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        [Route("{roomId}/door/close")]
        public async Task PostCloseDoor(string roomId)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            await AssertRoomIsFromOrg(room);
            await _conferenceRoomService.SecurityCheck(room);
            if (string.IsNullOrEmpty(room.GdoDeviceId))
            {
                throw new ArgumentException("No door to control");
            }
            await _gdoService.Close(room.GdoDeviceId);
        }
    }
}
