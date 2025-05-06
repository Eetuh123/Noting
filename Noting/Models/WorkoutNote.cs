using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using static Noting.Models.DatabaseManipulator;

namespace Noting.Models
{
    [BsonIgnoreExtraElements]
    public class WorkoutNote : IMongoDocument
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public ObjectId UserId { get; set; }

        public string NameTag { get; set; } = "";
        public DateTimeOffset Date { get; set; }

        public string NoteText { get; set; } = "";

        [BsonElement("exerciseIds")]
        public List<ObjectId> ExerciseIds { get; set; } = new();

        [BsonIgnore]
        public List<Exercise> Exercises { get; set; } = new();
    }
}
