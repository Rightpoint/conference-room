using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : ApiController
    {
        private readonly IConferenceRoomService _conferenceRoomService;
        private readonly IChangeNotificationService _changeNotificationService;

        public RoomController(IConferenceRoomService conferenceRoomService, IChangeNotificationService changeNotificationService)
        {
            _conferenceRoomService = conferenceRoomService;
            _changeNotificationService = changeNotificationService;
        }

        /// <summary>
        /// Gets the info for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/info")]
        public object GetInfo(string roomAddress, string securityKey)
        {
            _changeNotificationService.TrackRoom(roomAddress);
            return _conferenceRoomService.GetInfo(roomAddress, securityKey);
        }

        /// <summary>
        /// Gets the status for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/status")]
        public object GetStatus(string roomAddress)
        {
            _changeNotificationService.TrackRoom(roomAddress);
            return _conferenceRoomService.GetStatus(roomAddress);
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
        /// Warn attendees this meeting will be marked as abandoned (not started in time) very soon.
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomAddress}/meeting/warnAbandon")]
        public void PostWarnAbandonMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            _conferenceRoomService.WarnMeeting(roomAddress, uniqueId, securityKey);
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

        private string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
    }
}
