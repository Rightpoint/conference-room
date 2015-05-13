using System;
using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/room")]
    public class RoomController : ApiController
    {
        private readonly ExchangeService _exchangeService;

        public RoomController(ExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }

        /// <summary>
        /// Gets the schedule for a single room.
        /// </summary>
        /// <param name="id">The <see cref="Address"/> returned from <see cref="RoomListController.GetRooms"/></param>
        /// <returns></returns>
        [Route("{id}/schedule")]
        public object GetSchedule(string id)
        {
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(id));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            return cal.FindAppointments(new CalendarView(DateTime.Today, DateTime.Today.AddDays(2))).Select(i => new
            {
                i.Id,
                i.Subject,
                i.Start,
                i.End,
                Organizer = i.Organizer.Name,
            }).ToList();
        }
    }
}
