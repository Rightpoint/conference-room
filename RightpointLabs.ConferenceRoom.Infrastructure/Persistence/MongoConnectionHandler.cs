using System.Linq;
using MongoDB.Driver;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence
{
    public class MongoConnectionHandler : IMongoConnectionHandler
    {
        private readonly MongoDatabase _database;

        public MongoConnectionHandler(string connectionString) : this(connectionString, null)
        {
        }

        public MongoConnectionHandler(string connectionString, string database)
        {
            var mongoUrl = new MongoUrl(connectionString);
            _database = new MongoServer(MongoServerSettings.FromUrl(mongoUrl)).GetDatabase(database ?? mongoUrl.DatabaseName);
        }

        public MongoDatabase Database
        {
            get
            {
                return _database;
            }
        }
    }
}