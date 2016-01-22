using System;
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
                try
                {
                    BsonClassMap.RegisterClassMap<MeetingInfoValues>(
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
