using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using System;
using System.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class RoomMetadataRepository : EntityRepository<RoomMetadataEntity>, IRoomMetadataRepository
    {
        public RoomMetadataRepository(RoomMetadataEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public RoomMetadataEntity GetRoomInfo(string roomAddress)
        {
            return this.Collection.FindOne(Query<RoomMetadataEntity>.Where(i => i.Id == roomAddress));
        }

        public void SaveRoomInfo(string roomAddress, RoomMetadataEntity value)
        {
            var update = Update<RoomMetadataEntity>
                .Set(i => i.Id, roomAddress)
                .Set(i => i.Size, value.Size)
                .Set(i => i.BuildingId, value.BuildingId)
                .Set(i => i.Floor, value.Floor)
                .Set(i => i.DistanceFromFloorOrigin, value.DistanceFromFloorOrigin.Clone())
                .Set(i => i.Equipment, value.Equipment.ToList())
                .Set(i => i.GdoDeviceId, value.GdoDeviceId)
                .Set(i => i.LastModified, DateTime.Now);

            var result = this.Collection.Update(Query<RoomMetadataEntity>.Where(i => i.Id == roomAddress), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}