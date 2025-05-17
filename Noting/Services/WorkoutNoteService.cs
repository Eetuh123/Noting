using MongoDB.Bson;
using MongoDB.Driver;
using Noting.Models;

namespace Noting.Services
{
    public class WorkoutNoteService
    {
        private readonly ICurrentUserService _currentUser;
        public WorkoutNoteService(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }
        public WorkoutNote GetNote(ObjectId userId, string tag, DateTime date)
        {

            var all = DatabaseManipulator.database
                .GetCollection<WorkoutNote>(nameof(WorkoutNote))
                .Find(_ => true)
                .ToList();

            return all
                .FirstOrDefault(n =>
                    n.UserId == userId &&
                    n.NameTag == tag &&
                    n.Date.Date == date.Date);
        }

        public WorkoutNote SaveNote(WorkoutNote note)
        {
            return DatabaseManipulator.Save(note);
        }
        public async Task<List<WorkoutNote>> GetNotesForUserAsync()
        {
            var userId = await _currentUser.GetUserIdAsync();
            if (userId == null)
                return new List<WorkoutNote>();

            return await DatabaseManipulator
                .database
                .GetCollection<WorkoutNote>(nameof(WorkoutNote))
                .Find(n => n.UserId == userId)
                .SortByDescending(n => n.Date)
                .ToListAsync();
        }
        public async Task<WorkoutNote?> GetNoteByIdAsync(ObjectId id)
        {
            return await DatabaseManipulator
                .database
                .GetCollection<WorkoutNote>(nameof(WorkoutNote))
                .Find(n => n.Id == id)
                .FirstOrDefaultAsync();
        }
        public async Task DeleteNoteAsync(ObjectId noteId)
        {
            var note = await GetNoteByIdAsync(noteId);
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
