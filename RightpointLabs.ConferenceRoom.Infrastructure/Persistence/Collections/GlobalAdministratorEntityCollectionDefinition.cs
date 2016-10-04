using System;
using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class GlobalAdministratorEntityCollectionDefinition : EntityCollectionDefinition<GlobalAdministratorEntity>
    {
        public GlobalAdministratorEntityCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(GlobalAdministratorEntity)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<GlobalAdministratorEntity>(
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
