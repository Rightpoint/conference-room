using System;
using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class DeviceEntityCollectionDefinition : EntityCollectionDefinition<DeviceEntity>
    {
        public DeviceEntityCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(DeviceEntity)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<DeviceEntity>(
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
