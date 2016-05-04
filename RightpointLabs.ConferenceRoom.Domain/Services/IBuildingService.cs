using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IBuildingService
    {
        BuildingInfo Get(string buildingId);
        void Add(BuildingInfo buildingInfo);
        void Update(string buildingId, BuildingInfo buildingInfo);
    }
}
