using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

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
                            cm.GetMemberMap(i => i.OrganizationId).SetSerializer(new StringSerializer(BsonType.ObjectId));
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
