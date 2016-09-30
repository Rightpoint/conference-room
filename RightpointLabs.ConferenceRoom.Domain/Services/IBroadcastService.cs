using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IBroadcastService
    {
        void BroadcastUpdate(OrganizationEntity org, string roomAddress);
    }
}