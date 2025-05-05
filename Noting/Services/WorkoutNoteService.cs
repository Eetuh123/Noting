using MongoDB.Bson;
using MongoDB.Driver;
using Noting.Models;

namespace Noting.Services
{
    public class WorkoutNoteService
    {
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
        public async Task<List<WorkoutNote>> GetNotesForUserAsync(ObjectId userId)
        {
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
    }
}
