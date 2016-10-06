using System.Collections.Generic;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IConferenceRoomDiscoveryService
    {
        IEnumerable<RoomList> GetRoomLists();

        IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress);

        Task<string> GetRoomName(string roomAddress);
    }
}