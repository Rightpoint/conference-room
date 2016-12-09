using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IBroadcastService
    {
        void BroadcastUpdate(OrganizationEntity org, IRoom room);
        void BroadcastDeviceChange(OrganizationEntity org, DeviceEntity device);
        void BroadcastRefresh(OrganizationEntity org, DeviceEntity device = null);
    }
}