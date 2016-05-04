using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IBuildingRepository
    {
        BuildingInfo Get(string buildingId);
        void Save(string buildingId, BuildingInfo value);
    }
}