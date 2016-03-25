using log4net;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConferenceRoomService _conferenceRoomService;

        public RoomController(IConferenceRoomService conferenceRoomService)
            : base(__log)
        {
            _conferenceRoomService = conferenceRoomService;
        }

        /// <summary>
        /// Gets the info for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/info")]
        public object GetInfo(string roomAddress, string securityKey)
        {
            var data = _conferenceRoomService.GetInfo(roomAddress, securityKey);
            return data;
        }

        /// <summary>
        /// Sets the info metadata for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <param name="roomMetadata">The metadata associated with the room.</param>
        /// <returns></returns>
        [Route("{roomAddress}/info")]
        public void PostInfo(string roomAddress, PostRoomMetadata roomMetadata)
        {
            var realCode = ConfigurationManager.AppSettings["settingsSecurityCode"];
            if (roomMetadata.Code != realCode)
            {
                Thread.Sleep(1000);
                throw new AccessDeniedException("Access denied", null);
            }

            _conferenceRoomService.SetInfo(roomAddress, roomMetadata);
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
        /// Request access to control a room
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <param name="securityKey">The key that will be used to control the room in the future (if this request is approved)</param>
        [Route("{roomAddress}/requestAccess")]
        public void PostRequestAccess(string roomAddress, string securityKey)
        {
            _conferenceRoomService.RequestAccess(roomAddress, securityKey, GetClientIp());
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/start")]
        public void PostStartMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.StartMeeting(roomAddress, uniqueId, securityKey);
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
        public void PostWarnAbandonMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.WarnMeeting(roomAddress, uniqueId, securityKey,
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
        public void PostAbandonMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.CancelMeeting(roomAddress, uniqueId, securityKey);
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
        /// <param name="minutes">The length of the meeting in minutes</param>
        [Route("{roomAddress}/meeting/startNew")]
        public void PostStartNewMeeting(string roomAddress, string securityKey, string title, int minutes)
        {
            _conferenceRoomService.StartNewMeeting(roomAddress, securityKey, title, minutes);
        }

        /// <summary>
        /// Marks a meeting as ended early.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/end")]
        public void PostEndMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.EndMeeting(roomAddress, uniqueId, securityKey);
        }

        [Route("{roomAddress}/meeting/message")]
        public void PostMessageMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.MessageMeeting(roomAddress, uniqueId, securityKey);
        }
    }
}
