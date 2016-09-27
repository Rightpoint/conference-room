using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IBuildingService
    {
        BuildingEntity Get(string buildingId);
        void Add(BuildingEntity buildingInfo);
        void Update(string buildingId, BuildingEntity buildingInfo);
    }
}
