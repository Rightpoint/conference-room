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
                BsonClassMap.RegisterClassMap<SecurityRequest>(
                    cm =>
                    {
                        cm.AutoMap();
                    });
            }
        }
    }
}
