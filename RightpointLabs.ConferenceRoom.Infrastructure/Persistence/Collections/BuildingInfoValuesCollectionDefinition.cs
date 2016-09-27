using System;
using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class BuildingEntityCollectionDefinition : EntityCollectionDefinition<BuildingEntity>
    {
        public BuildingEntityCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(BuildingEntity)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<BuildingEntity>(
                        cm =>
                        {
                            cm.AutoMap();
                        });
                }
                catch (ArgumentException)
                {
                    // this fails with an argument exception at startup, but otherwise works fine.  Probably should try to figure out why, but ignoring it is easier :(
                }
            }
        }
    }
}
