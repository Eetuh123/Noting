using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Noting.Models;
using Noting.Services;

namespace NotingTesting.Services
{
    public class TokenParseTester
    {
        private ExerciseService CreateServiceWithMocks(
            out Mock<IMongoCollection<Exercise>> mockCollection,
            Mock<ICurrentUserService> mockUser)
        {
            mockCollection = new Mock<IMongoCollection<Exercise>>();
            mockCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<Exercise>>(),
                    It.IsAny<Exercise>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(ReplaceOneResult));

            var mockDb = new Mock<IMongoDatabase>();
            mockDb
                .Setup(db => db.GetCollection<Exercise>(
                    nameof(Exercise),
                    It.IsAny<MongoCollectionSettings>()))
                .Returns(mockCollection.Object);

            DatabaseManipulator.database = mockDb.Object;

            return new ExerciseService(mockUser.Object);
        }

        [Fact]
        public async Task SaveFromText_NameWeightRepsSeparatorSetsNotes_ParsesCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            // Arrange input
            var rawText = "bench press 50kg 6x5 cool";
            var noteDate = DateTimeOffset.UtcNow;

            // Act
            var result = await service.SaveFromText(rawText, noteDate);

            // Assert parsing fields
            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            // Project RepEntry.Reps (int?) to int for easy comparison
            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 6, 6, 6, 6, 6 }, reps);

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
        [Fact]
        public async Task SaveFromText_WeightUnitNameSymetricRepsSeparatorSetsNotes_ParsesCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);


            var rawText = "50kg bench press 5x5 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 5, 5 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameRepsSeparatorNotes_ParsesCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);


            var rawText = "50 bench press 12x15 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 12, 15 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameSetsSeparatorRepsNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "50 bench press 2x8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameRepsSetsNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "50 bench press 8 2 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightUnitNameReps3xNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "50kg bench press 8 8 8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameReps3xNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "50 bench press 8 8 8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameRepsWithSeparators3xNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "50 bench press 8x8x8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameReps3xWeightNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 8 8 8 50 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameWeightRepsWithSeparatorNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 50 8/8/8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_WeightNameRepsSeparator3x_ParsesCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);


            var rawText = "50 bench press 10x12x10";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 10, 12, 10 }, reps);

            Assert.Equal("", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameRepsWithSeparatorWeightNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 8/8/8 50 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameRepsWithSeparatorNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 8/8/8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(0, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameRepsNotes_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 8 8 8 cool";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(0 ,result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 8, 8, 8 }, reps);

            Assert.Equal("cool", result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
        [Fact]
        public async Task SaveFromText_NameWeightUnitReps_ParseCorrectly()
        {
            var userId = ObjectId.GenerateNewId();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.GetUserIdAsync()).ReturnsAsync(userId);

            var service = CreateServiceWithMocks(out var mockCollection, mockUser);

            var rawText = "bench press 50kg 5x6";
            var noteDate = DateTimeOffset.UtcNow;

            var result = await service.SaveFromText(rawText, noteDate);

            Assert.Equal("bench press", result.NameTag);
            Assert.Equal(50, result.Weight);

            var reps = result.Reps
                             .Select(r => r.Reps.GetValueOrDefault())
                             .ToArray();
            Assert.Equal(new[] { 6, 6, 6, 6, 6 }, reps);

            Assert.Equal("" ,result.Notes);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(rawText.Trim(), result.RawText);
            Assert.Equal(noteDate, result.Date);

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
