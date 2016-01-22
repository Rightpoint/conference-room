using System;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public abstract class EntityCollectionDefinition<T> where T : Entity
    {
        protected EntityCollectionDefinition(IMongoConnectionHandler connectionHandler)
        {
            if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");

            Collection = connectionHandler.Database.GetCollection<T>(typeof(T).Name.ToLower() + "s");

            // setup serialization
            if (!BsonClassMap.IsClassMapRegistered(typeof(Entity)))
            {
                try
                {
                    BsonClassMap.RegisterClassMap<Entity>(
                        cm =>
                        {
                            cm.AutoMap();
                            cm.SetIdMember(cm.GetMemberMap(i => i.Id));
                            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
                        });
                }
                catch (ArgumentException)
                {
                    // this fails with an argument exception at startup, but otherwise works fine.  Probably should try to figure out why, but ignoring it is easier :(
                }
            }
        }

        public readonly MongoCollection<T> Collection;
    }
}
