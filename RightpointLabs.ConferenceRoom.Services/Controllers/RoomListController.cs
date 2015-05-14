using System.Linq;
using System.Web.Http;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with room lists
    /// </summary>
    [RoutePrefix("api/roomList")]
    public class RoomListController : ApiController
    {
        private readonly IConferenceRoomService _conferenceRoomService;

        public RoomListController(IConferenceRoomService conferenceRoomService)
        {
            _conferenceRoomService = conferenceRoomService;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public object GetAll()
        {
            return _conferenceRoomService.GetRoomLists();
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetAll"/></param>
        /// <returns></returns>
        [Route("{roomListAddress}/rooms")]
        [HttpGet]
        public object GetRooms(string roomListAddress)
        {
            return _conferenceRoomService.GetRoomsFromRoomList(roomListAddress);
        }
    }
}
