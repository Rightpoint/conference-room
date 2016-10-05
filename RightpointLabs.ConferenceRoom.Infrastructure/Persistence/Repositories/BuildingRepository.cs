using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class BuildingRepository : EntityRepository<BuildingEntity>, IBuildingRepository
    {
        public BuildingRepository(BuildingEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public BuildingEntity Get(string buildingId)
        {
            return this.Collection.FindOne(Query<BuildingEntity>.Where(i => i.Id == buildingId));
        }

        public IEnumerable<BuildingEntity> GetAll(string organizationId)
        {
            return this.Collection.Find(Query<BuildingEntity>.Where(i => i.OrganizationId == organizationId));
        }

        public void Save(string buildingId, BuildingEntity value)
        {
            var update = Update<BuildingEntity>
                .Set(i => i.Id, buildingId)
                .Set(i => i.Name, value.Name)
                .Set(i => i.StreetAddress1, value.StreetAddress1)
                .Set(i => i.StreetAddress2, value.StreetAddress2)
                .Set(i => i.City, value.City)
                .Set(i => i.StateOrProvence, value.StateOrProvence)
                .Set(i => i.PostalCode, value.PostalCode)
                .Set(i => i.LastModified, DateTime.Now);

            var result = this.Collection.Update(Query<BuildingEntity>.Where(i => i.Id == buildingId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}