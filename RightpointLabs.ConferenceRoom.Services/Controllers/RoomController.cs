using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : ApiController
    {
        private readonly IConferenceRoomService _conferenceRoomService;

        public RoomController(IConferenceRoomService conferenceRoomService)
        {
            _conferenceRoomService = conferenceRoomService;
        }

        /// <summary>
        /// Gets the schedule for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/schedule")]
        public object GetSchedule(string roomAddress)
        {
            return _conferenceRoomService.GetUpcomingAppointmentsForRoom(roomAddress);
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        /// <returns></returns>
        [Route("{roomAddress}/meeting/start")]
        public object PostStartMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marks a meeting as abandoned (not started in time).
        /// A client *must* call this.  If we lost connectivity to a client at a room, we'd rather let meetings continue normally than start cancelling them with no way for people to stop it.
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <param name="securityKey">The client's security key (indicating it is allowed to do this)</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        /// <returns></returns>
        [Route("{roomAddress}/meeting/abandon")]
        public object PostAbandonMeeting(string roomAddress, string securityKey, string uniqueId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get room status
        /// </summary>
        /// <param name="roomAddress">The address of the room</param>
        /// <returns></returns>
        [Route("{roomAddress}/status")]
        public object GetStatus(string roomAddress)
        {
            var now = DateTime.Now;
            var current = _conferenceRoomService.GetUpcomingAppointmentsForRoom(roomAddress)
                    .OrderBy(i => i.Start)
                    .FirstOrDefault(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now);

            if (null == current)
            {
                return new
                {
                    Status = RoomStatus.Free,
                    NextChangeSeconds = 15*60,
                };
            }
            else if (now < current.Start)
            {
                return new
                {
                    Status = RoomStatus.Free,
                    NextChangeSeconds = Math.Min(15 * 60, current.Start.Subtract(now).TotalSeconds),
                    Meeting = current,
                };
            }
            else
            {
                return new
                {
                    Status = "busy",
                    NextChangeSeconds = current.End.Subtract(now).TotalSeconds,
                    Meeting = current,
                };
            }
        }
    }
}
