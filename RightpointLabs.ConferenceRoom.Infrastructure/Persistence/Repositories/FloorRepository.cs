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
    public class FloorRepository : EntityRepository<FloorInfoValues>, IFloorRepository
    {
        public FloorRepository(FloorInfoValuesCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public FloorInfo GetFloorInfo(string floorId)
        {
            return this.Collection.FindOne(Query<FloorInfoValues>.Where(i => i.Id == floorId));
        }
    }
}