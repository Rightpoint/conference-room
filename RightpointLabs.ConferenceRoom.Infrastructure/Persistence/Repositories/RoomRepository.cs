using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;
using System;
using System.Linq;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class RoomRepository : EntityRepository<RoomInfoValues>, IRoomRepository
    {
        public RoomRepository(RoomInfoValuesCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public RoomMetadata GetRoomInfo(string roomAddress)
        {
            return this.Collection.FindOne(Query<RoomInfoValues>.Where(i => i.Id == roomAddress));
        }

        public void SaveRoomInfo(string roomAddress, RoomMetadata value)
        {
            var update = Update<RoomInfoValues>
                .Set(i => i.Id, roomAddress)
                .Set(i => i.Size, value.Size)
                .Set(i => i.BuildingId, value.BuildingId)
                .Set(i => i.Floor, value.Floor)
                .Set(i => i.DistanceFromFloorOrigin, value.DistanceFromFloorOrigin.Clone())
                .Set(i => i.Equipment, value.Equipment.ToList())
                .Set(i => i.GdoDeviceId, value.GdoDeviceId)
                .Set(i => i.LastModified, DateTime.Now);

            var result = this.Collection.Update(Query<RoomInfoValues>.Where(i => i.Id == roomAddress), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}