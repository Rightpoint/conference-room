using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
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

            // ReSharper disable once VirtualMemberCallInConstructor
            Collection = connectionHandler.Database.GetCollection<T>(CollectionName, new MongoCollectionSettings() { AssignIdOnInsert = true});

            // setup serialization
            if (!BsonClassMap.IsClassMapRegistered(typeof(Entity)))
            {
                if (!Collection.Exists())
                {
                    // ReSharper disable once VirtualMemberCallInConstructor
                    Collection.Database.CreateCollection(CollectionName);
                }

                try
                {
                    BsonClassMap.RegisterClassMap<Entity>(
                        cm =>
                        {
                            cm.AutoMap();
                            cm.SetIdMember(cm.GetMemberMap(i => i.Id));
                            cm.IdMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance).SetSerializer(new StringSerializer(BsonType.ObjectId));
                        });
                }
                catch (ArgumentException)
                {
                    // this fails with an argument exception at startup, but otherwise works fine.  Probably should try to figure out why, but ignoring it is easier :(
                }
            }
        }

        public readonly MongoCollection<T> Collection;

        protected virtual string CollectionName => typeof(T).Name.Replace("Entity", "").ToLower() + "s";
    }
}
