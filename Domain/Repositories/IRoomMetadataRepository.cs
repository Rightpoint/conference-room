using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IRoomMetadataRepository : IRepository
    {
        RoomMetadataEntity GetRoomInfo(string roomId);
        IEnumerable<RoomMetadataEntity> GetRoomInfosForBuilding(string buildingId);
        IEnumerable<RoomMetadataEntity> GetRoomInfosForOrganization(string organizationId);
        void Update(RoomMetadataEntity model);
    }
}