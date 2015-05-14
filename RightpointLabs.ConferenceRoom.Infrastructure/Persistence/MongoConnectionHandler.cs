using MongoDB.Driver;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence
{
    public class MongoConnectionHandler : IMongoConnectionHandler
    {
        private readonly MongoDatabase _database;

         public MongoConnectionHandler(string connectionString, string database)
         {
             _database = new MongoServer(MongoServerSettings.FromUrl(new MongoUrl(connectionString))).GetDatabase(database);
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