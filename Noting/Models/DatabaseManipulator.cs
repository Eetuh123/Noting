using MongoDB.Driver;
using MongoDB.Bson;

namespace Noting.Models
{
    public class DatabaseManipulator
    {
        private static IConfiguration? config;
        private static string? DATABASE_NAME;
        private static string? HOST;
        private static MongoServerAddress? address;
        private static MongoClientSettings? settings;
        private static MongoClient? client;
        public static IMongoDatabase? database;

        public static void Initialize(IConfiguration Configuration)
        {
            config = Configuration;
            var sections = config.GetSection("ConnectionStrings");
            DATABASE_NAME = sections.GetValue<string>("DatabaseName");
            HOST = sections.GetValue<string>("MongoConnection");
            address = new MongoServerAddress(HOST);
            settings = new MongoClientSettings() { Server = address };
            client = new MongoClient(settings);
            database = client.GetDatabase(DATABASE_NAME);
        }
        public static T Save<T>(T record) where T : IMongoDocument
        {

            var collection = database.GetCollection<T>(typeof(T).Name);
            var filter = Builders<T>.Filter.Eq("Id", record.Id);

            collection.ReplaceOne(filter, record, new ReplaceOptions { IsUpsert = true });
            return record;

        }
        public interface IMongoDocument
        {
            ObjectId Id { get; set; }
        }
    }
}
