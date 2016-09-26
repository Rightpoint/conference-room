using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IFloorRepository
    {
        FloorInfo GetFloorInfo(string floorId);
    }
}