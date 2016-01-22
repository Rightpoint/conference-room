using System;
using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class SecurityRequestCollectionDefinition : EntityCollectionDefinition<SecurityRequest>
    {
        public SecurityRequestCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(SecurityRequest)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<SecurityRequest>(
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
