using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    public class RoomListController : ApiController
    {
        private readonly ExchangeService _exchangeService;

        public RoomListController(ExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }

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

        [Route("{id}/rooms")]
        public object GetRooms(string id)
        {
            return _exchangeService.GetRooms(id).Select(i => new
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
