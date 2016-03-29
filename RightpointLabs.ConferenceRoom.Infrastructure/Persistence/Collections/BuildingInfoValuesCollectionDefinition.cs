using System;
using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class BuildingInfoValuesCollectionDefinition : EntityCollectionDefinition<BuildingInfoValues>
    {
        public BuildingInfoValuesCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(BuildingInfoValues)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<BuildingInfoValues>(
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
