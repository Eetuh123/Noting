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

        [Required(ErrorMessage = "Please enter your name")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Please enter your email")]
        [EmailAddress(ErrorMessage = "That doesn’t look like a valid email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password")]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6,
        ErrorMessage = "The {0} must be between {2} and {1} characters long")]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; }

        public DateTime? LastLogin { get; set; }
    }
}
