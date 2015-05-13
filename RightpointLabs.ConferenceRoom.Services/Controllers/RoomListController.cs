using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with room lists
    /// </summary>
    [RoutePrefix("api/roomList")]
    public class RoomListController : ApiController
    {
        private readonly ExchangeService _exchangeService;

        public RoomListController(ExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public object GetAll()
        {
            return _exchangeService.GetRoomLists().Select(i => new
            {
                i.Id,
                i.Address,
                i.MailboxType,
                i.Name,
                i.RoutingType
            }).ToList();
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetAll"/></param>
        /// <returns></returns>
        [Route("{id}/rooms")]
        [HttpGet]
        public object GetRooms(string roomListAddress)
        {
            return _exchangeService.GetRooms(roomListAddress).Select(i => new
            {
                i.Id,
                i.Address,
                i.MailboxType,
                i.Name,
                i.RoutingType
            }).ToList();
        }
    }
}
