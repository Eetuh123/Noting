using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Noting.Models;
using Noting.Services;
using static Noting.Models.DatabaseManipulator;   // for the static .database

namespace NotingTesting.Services
{
    public class ExerciseServiceTests
    {
        [Fact]
        public async Task SaveFromText_BenchPress50kg5x5Cool_ParsesCorrectly()
        {
            // 1) Mock the Mongo Collection
            var mockCollection = new Mock<IMongoCollection<Exercise>>();
            mockCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<Exercise>>(),
                    It.IsAny<Exercise>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>())
                )
                // return a default/null ReplaceOneResult so the Task<T> completes
                .ReturnsAsync(default(ReplaceOneResult));

            // 2) Mock the database to return that collection
            var mockDb = new Mock<IMongoDatabase>();
            mockDb
                .Setup(db => db.GetCollection<Exercise>(
                    nameof(Exercise),
                    It.IsAny<MongoCollectionSettings>()))
                .Returns(mockCollection.Object);

            // 3) Hijack the static DatabaseManipulator.database
            database = mockDb.Object;

            // 4) Use the parameterless ctor now that static is set
            var service = new ExerciseService();

            // Arrange input
            var rawText = "bench press 50kg 5x5 cool";
            var userId = ObjectId.GenerateNewId();
            var noteDate = DateTimeOffset.UtcNow;

            // Act
            var result = await service.SaveFromText(rawText, userId, noteDate);

            // Assert parsing fields
            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            // Project RepEntry.Reps (int?) to int for easy comparison
            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 5, 5 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

            // Verify that we upserted exactly once
            mockCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Exercise>>(),
                It.Is<Exercise>(e =>
                    e.Id == result.Id &&
                    e.UserId == userId &&
                    e.NameTag == result.NameTag
                ),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }
}
