using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class OrganizationServiceConfigurationEntityCollectionDefinition : EntityCollectionDefinition<OrganizationServiceConfigurationEntity>
    {
        public OrganizationServiceConfigurationEntityCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(OrganizationServiceConfigurationEntity)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<OrganizationServiceConfigurationEntity>(
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
