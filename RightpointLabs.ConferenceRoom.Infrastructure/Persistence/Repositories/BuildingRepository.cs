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
    public class BuildingRepository : EntityRepository<BuildingInfoValues>, IBuildingRepository
    {
        public BuildingRepository(BuildingInfoValuesCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public BuildingInfo Get(string buildingId)
        {
            return this.Collection.FindOne(Query<BuildingInfoValues>.Where(i => i.Id == buildingId));
        }

        public void Save(string buildingId, BuildingInfo value)
        {
            var update = Update<BuildingInfoValues>
                .Set(i => i.Id, buildingId)
                .Set(i => i.Name, value.Name)
                .Set(i => i.StreetAddress1, value.StreetAddress1)
                .Set(i => i.StreetAddress2, value.StreetAddress2)
                .Set(i => i.City, value.City)
                .Set(i => i.StateOrProvence, value.StateOrProvence)
                .Set(i => i.PostalCode, value.PostalCode)
                .Set(i => i.Floors, value.Floors.Select(_ => _.Clone()).ToList())
                .Set(i => i.LastModified, DateTime.Now);

            var result = this.Collection.Update(Query<BuildingInfoValues>.Where(i => i.Id == buildingId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}