using MongoDB.Bson;
using MongoDB.Driver;
using Noting.Models;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Noting.Services
{
    public class WorkoutNoteService
    {
        private readonly ICurrentUserService _currentUser;
        public WorkoutNoteService(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }
        public WorkoutNote SaveNote(WorkoutNote note)
        {
            return DatabaseManipulator.Save(note);
        }
        public async Task<List<WorkoutNote>> GetNotesForUserAsync(ObjectId userId)
        {
            if (userId == null)
                return new List<WorkoutNote>();

            return await DatabaseManipulator
                .database
                .GetCollection<WorkoutNote>(nameof(WorkoutNote))
                .Find(n => n.UserId == userId)
                .SortByDescending(n => n.Date)
                .ToListAsync();
        }
        public async Task<WorkoutNote?> GetNoteByIdAsync(ObjectId id, ObjectId userId)
        {
            return await DatabaseManipulator
            .database
                .GetCollection<WorkoutNote>(nameof(WorkoutNote))
                .Find(n => n.Id == id && n.UserId == userId)
                .FirstOrDefaultAsync();
        }
        public async Task DeleteNoteAsync(ObjectId noteId)
        {
            var userId = await _currentUser.GetUserIdAsync();
            var note = await GetNoteByIdAsync(noteId, userId.Value);
            if (note == null) return;

            if (note.ExerciseIds?.Any() == true)
            {
                var filter = Builders<Exercise>.Filter.In(e => e.Id, note.ExerciseIds);
                var exercisesCol = DatabaseManipulator.database
                                        .GetCollection<Exercise>(nameof(Exercise));
                await exercisesCol.DeleteManyAsync(filter);
            }
            var notesCol = DatabaseManipulator.database
                                  .GetCollection<WorkoutNote>(nameof(WorkoutNote));
            var noteFilter = Builders<WorkoutNote>
                                 .Filter
                                 .Eq(n => n.Id, noteId);
            await notesCol.DeleteOneAsync(noteFilter);
        }
    }
}
