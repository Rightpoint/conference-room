using MongoDB.Driver;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence
{
    public interface IMongoConnectionHandler
    {
        MongoDatabase Database { get; }
    }
}