using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IBuildingRepository
    {
        BuildingEntity Get(string buildingId);
        IEnumerable<BuildingEntity> GetAll(string organizationId);
        void Save(string buildingId, BuildingEntity value);
    }
}