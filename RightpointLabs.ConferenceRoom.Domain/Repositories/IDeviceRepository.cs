using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IDeviceRepository
    {
        DeviceEntity Create(DeviceEntity entity);

        DeviceEntity Get(string deviceId);

        void Save(DeviceEntity device);
    }
}