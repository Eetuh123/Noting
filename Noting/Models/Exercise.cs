using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Claims;
using static Noting.Models.DatabaseManipulator;

namespace Noting.Models
{
    public class Exercise : IMongoDocument
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public ObjectId UserId { get; set; } = ObjectId.Empty;
        public required string NameTag { get; set; }
        public required int Weight { get; set; }
        public List<RepEntry> Reps { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        public required DateTimeOffset Date { get; set; }
        public required string RawText { get; set; }

        public bool TrySetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ObjectId.TryParse(userIdString, out var id))
            {
                UserId = id;
                return true;
            }
            return false;
        }

    }
}
