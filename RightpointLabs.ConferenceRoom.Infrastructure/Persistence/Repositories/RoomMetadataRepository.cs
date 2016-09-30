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

        public RoomMetadataEntity GetRoomInfo(string roomAddress, string organizationId)
        {
            var q = Query<RoomMetadataEntity>.Where(i => i.RoomAddress == roomAddress && i.OrganizationId == organizationId);
            return this.Collection.FindOne(q);
        }
    }
}