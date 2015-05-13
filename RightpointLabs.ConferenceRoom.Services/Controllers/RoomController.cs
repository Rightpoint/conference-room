using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Services.Models;
using RightpointLabs.ConferenceRoom.Services.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : ApiController
    {
        private readonly ExchangeService _exchangeService;
        private readonly IMeetingRepository _meetingRepository;

        public RoomController(ExchangeService exchangeService, IMeetingRepository meetingRepository)
        {
            _exchangeService = exchangeService;
            _meetingRepository = meetingRepository;
        }

        /// <summary>
        /// Gets the schedule for a single room.
        /// </summary>
        /// <param name="roomAddress">The room address returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{roomAddress}/schedule")]
        public object GetSchedule(string roomAddress)
        {
            return LoadSoonAppointments(roomAddress);
        }

        private List<Meeting> LoadSoonAppointments(string roomAddress)
        {
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var apt = cal.FindAppointments(new CalendarView(DateTime.Today, DateTime.Today.AddDays(2))).ToList();
            var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.UniqueId);
            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingInfo() {  UniqueId = i.Id.UniqueId } )).ToList();
        }

        private static Meeting BuildMeeting(Appointment i, MeetingInfo meetingInfo)
        {
            return new Meeting {Id = i.Id, Subject = i.Subject, Start = i.Start, End = i.End, Organizer = i.Organizer.Name, IsStarted = meetingInfo.IsStarted, IsEndedEarly = meetingInfo.IsEndedEarly, IsCancelled = meetingInfo.IsCancelled};
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
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var now = DateTime.Now;
            var current =LoadSoonAppointments(roomAddress)
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
