using System.Reflection;
using System.Web.Http;
using log4net;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
{
    /// <summary>
    /// Operations dealing with room lists
    /// </summary>
    [RoutePrefix("api/roomList")]
    public class RoomListController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConferenceRoomDiscoveryService _conferenceRoomDiscoveryService;

        public RoomListController(IConferenceRoomDiscoveryService conferenceRoomDiscoveryService)
            : base(__log)
        {
            _conferenceRoomDiscoveryService = conferenceRoomDiscoveryService;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public object GetAll()
        {
            return _conferenceRoomDiscoveryService.GetRoomLists();
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
            return _conferenceRoomDiscoveryService.GetRoomsFromRoomList(roomListAddress);
        }
    }
}
