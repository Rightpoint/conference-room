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
    public class FloorRepository : EntityRepository<FloorEntity>, IFloorRepository
    {
        public FloorRepository(FloorEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public FloorEntity Get(string floorId)
        {
            return this.Collection.FindOne(Query<FloorEntity>.Where(i => i.Id == floorId));
        }

        public IEnumerable<FloorEntity> GetAllByOrganization(string organizationId)
        {
            return this.Collection.Find(Query<FloorEntity>.Where(i => i.OrganizationId == organizationId));
        }

        public IEnumerable<FloorEntity> GetAllByBuilding(string buildingId)
        {
            return this.Collection.Find(Query<FloorEntity>.Where(i => i.BuildingId == buildingId));
        }
    }
}