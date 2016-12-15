using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IBuildingRepository : IRepository
    {
        BuildingEntity Get(string buildingId);
        IEnumerable<BuildingEntity> GetAll(string organizationId);
        void Update(BuildingEntity value);
    }
}