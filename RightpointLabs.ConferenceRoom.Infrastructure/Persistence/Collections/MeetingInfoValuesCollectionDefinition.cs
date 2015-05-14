using MongoDB.Bson.Serialization;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections
{
    public class MeetingInfoValuesCollectionDefinition : EntityCollectionDefinition<MeetingInfoValues>
    {
        public MeetingInfoValuesCollectionDefinition(IMongoConnectionHandler connectionHandler)
            : base(connectionHandler)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(MeetingInfoValues)))
            {
                BsonClassMap.RegisterClassMap<MeetingInfoValues>(
                    cm =>
                    {
                        cm.AutoMap();
                    });
            }
        }
    }
}
