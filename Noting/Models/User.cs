using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using static Noting.Models.DatabaseManipulator;

namespace Noting.Models
{
    public class User : IMongoDocument
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        [Required(ErrorMessage = "Name is required")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "Email is required")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public required string PasswordHash { get; set; }

        public DateTime? LastLogin { get; set; }
    }
}
