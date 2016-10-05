using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using log4net;
using RightpointLabs.ConferenceRoom.Domain;
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

        /// <summary>
        /// Gets the info for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/info")]
        public object GetInfo(string roomAddress)
        {
            var data = _conferenceRoomService.GetInfo(roomAddress);
            return data;
        }

        /// <summary>
        /// Gets the status for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/status")]
        public object GetStatus(string roomAddress)
        {
            var data = _conferenceRoomService.GetStatus(roomAddress);
            return data;
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/start")]
        public void PostStartMeeting(string roomAddress, string uniqueId)
        {
            _conferenceRoomService.StartMeeting(roomAddress, uniqueId);
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="signature">The signature of the uniqueId - indicating it's allowed to do this</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/startFromClient", Name = "StartFromClient")]
        public string GetStartMeeting(string roomAddress, string uniqueId, string signature)
        {
            if (_conferenceRoomService.StartMeetingFromClient(roomAddress, uniqueId, signature))
            {
                return "Meeting started";
            }
            else
            {
                return "Invalid link - please use the device on the outside of the room";
            }
        }

        /// <summary>
        /// Warn attendees this meeting will be marked as abandoned (not started in time) very soon.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/warnAbandon")]
        public void PostWarnAbandonMeeting(string roomAddress, string uniqueId)
        {
            _conferenceRoomService.WarnMeeting(roomAddress, uniqueId,
                signature => new Uri(Request.RequestUri, Url.Route("StartFromClient", new { roomAddress, uniqueId, signature })).AbsoluteUri,
                signature => new Uri(Request.RequestUri, Url.Route("CancelFromClient", new { roomAddress, uniqueId, signature })).AbsoluteUri);
        }

        /// <summary>
        /// Marks a meeting as abandoned (not started in time).
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/abandon")]
        public void PostAbandonMeeting(string roomAddress, string uniqueId)
        {
            _conferenceRoomService.CancelMeeting(roomAddress, uniqueId);
        }

        /// <summary>
        /// Marks a meeting as cancelled
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="signature">The signature of the uniqueId - indicating it's allowed to do this</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/abandonFromClient", Name = "CancelFromClient")]
        public string GetCancelMeeting(string roomAddress, string uniqueId, string signature)
        {
            if (_conferenceRoomService.CancelMeetingFromClient(roomAddress, uniqueId, signature))
            {
                return "Meeting abandoned";
            }
            else
            {
                return "Invalid link - please use the device on the outside of the room";
            }
        }

        /// <summary>
        /// Start a new meeting
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="title">The title of the meeting</param>
        /// <param name="endTime">The time the meeting will end</param>
        [Route("{roomAddress}/meeting/startNew")]
        public void PostStartNewMeeting(string roomAddress, string title, DateTime endTime)
        {
            _conferenceRoomService.StartNewMeeting(roomAddress, title, endTime);
        }

        /// <summary>
        /// Marks a meeting as ended early.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/end")]
        public void PostEndMeeting(string roomAddress, string uniqueId)
        {
            _conferenceRoomService.EndMeeting(roomAddress, uniqueId);
        }

        [Route("{roomAddress}/meeting/message")]
        public void PostMessageMeeting(string roomAddress, string uniqueId)
        {
            _conferenceRoomService.MessageMeeting(roomAddress, uniqueId);
        }

        [Route("all/status/{buildingId}")]
        public object GetAllStatus(string buildingId)
        {
            var building = _buildingRepository.Get(buildingId ?? _contextService.CurrentDevice?.BuildingId);
            var roomAddresses =
                (building?.RoomListAddresses ?? new string[0])
                    .AsParallel()
                    .WithDegreeOfParallelism(256)
                    .SelectMany(_conferenceRoomService.GetRoomsFromRoomList)
                    .ToList();

            var roomInfos = _conferenceRoomService.GetInfoForRoomsInBuilding(buildingId, roomAddresses.Select(i => i.Address).ToArray());
            var rooms =
                roomAddresses
                    .Select(i => new { i.Address, Info = roomInfos.TryGetValue(i.Address) })
                    .ToList();
            
            // ok, we have the filtered rooms list, now we need to get the status and smash it together with the room data
            return
                rooms.AsParallel()
                    .WithDegreeOfParallelism(256)
                    .Select(i =>
                    {
                        //__log.DebugFormat("Starting {0}", i.Address);
                        var status = _conferenceRoomService.GetStatus(i.Address, true);
                        //__log.DebugFormat("Got {0}", i.Address);
                        return new {i.Address, i.Info, Status = status};
                    })
                    .ToList();
        }

        /// <summary>
        /// Open a GDO
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        [Route("{roomAddress}/door/open")]
        public async System.Threading.Tasks.Task PostOpenDoor(string roomAddress)
        {
            _conferenceRoomService.SecurityCheck(roomAddress);
            var info = _roomRepository.GetRoomInfo(roomAddress, _contextService.CurrentOrganization?.Id);
            if (string.IsNullOrEmpty(info.GdoDeviceId))
            {
                throw new ArgumentException("No door to control");
            }
            await _gdoService.Open(info.GdoDeviceId);
        }

        /// <summary>
        /// Close a GDO
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        [Route("{roomAddress}/door/close")]
        public async System.Threading.Tasks.Task PostCloseDoor(string roomAddress)
        {
            _conferenceRoomService.SecurityCheck(roomAddress);
            var info = _roomRepository.GetRoomInfo(roomAddress, _contextService.CurrentOrganization?.Id);
            if (string.IsNullOrEmpty(info?.GdoDeviceId))
            {
                throw new ArgumentException("No door to control");
            }
            await _gdoService.Close(info.GdoDeviceId);
        }
    }
}
