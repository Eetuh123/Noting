using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using static Noting.Models.DatabaseManipulator;

namespace Noting.Models
{
    public class WorkoutNote : IMongoDocument
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public ObjectId UserId { get; set; }

        public string NameTag { get; set; } = "";
        public DateTime Date { get; set; }

        public string NoteText { get; set; } = "";
        public List<Exercise> Exercises { get; set; } = new();
    }
}
