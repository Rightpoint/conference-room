using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IRoomRepository
    {
        RoomMetadata GetRoomInfo(string roomAddress);
        void SaveRoomInfo(string roomAddress, RoomMetadata value);
    }

}