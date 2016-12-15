using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class DeviceRepository : TableByOrganizationRepository<DeviceEntity>, IDeviceRepository
    {
        public DeviceRepository(CloudTableClient client)
            : base(client)
        {
        }

        public DeviceEntity Create(DeviceEntity entity)
        {
            Insert(entity);
            return entity;
        }

        public DeviceEntity Get(string deviceId)
        {
            return this.GetById(deviceId);
        }

        public IEnumerable<DeviceEntity> GetForOrganization(string organizationId)
        {
            return GetAll(organizationId);
        }
    }
}