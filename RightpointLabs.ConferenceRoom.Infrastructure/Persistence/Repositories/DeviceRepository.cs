using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class DeviceRepository : EntityRepository<DeviceEntity>, IDeviceRepository
    {
        public DeviceRepository(DeviceEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public DeviceEntity Create(DeviceEntity entity)
        {
            var result = this.Collection.Insert(entity);
            AssertAffected(result, 1);
            return Get(entity.Id);
        }

        public DeviceEntity Get(string deviceId)
        {
            return this.Collection.FindOne(Query<DeviceEntity>.Where(i => i.Id == deviceId));
        }

        public void Save(DeviceEntity device)
        {
            var result = this.Collection.Update(Query<DeviceEntity>.Where(i => i.Id == device.Id), Update<DeviceEntity>.Replace(device));
            AssertAffected(result, 1);
        }
    }
}