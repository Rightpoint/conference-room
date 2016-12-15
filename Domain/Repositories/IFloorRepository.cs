using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IFloorRepository : IRepository
    {
        FloorEntity Get(string floorId);
        IEnumerable<FloorEntity> GetAllByOrganization(string organizationId);
        IEnumerable<FloorEntity> GetAllByBuilding(string buildingId);
        void Update(FloorEntity value);
    }
}