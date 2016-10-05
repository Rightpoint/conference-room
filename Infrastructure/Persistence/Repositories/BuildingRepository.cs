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
    }
}