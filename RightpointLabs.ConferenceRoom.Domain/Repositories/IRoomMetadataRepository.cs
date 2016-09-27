using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IRoomMetadataRepository
    {
        RoomMetadataEntity GetRoomInfo(string roomAddress);
        void SaveRoomInfo(string roomAddress, RoomMetadataEntity value);
    }

}