using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Noting.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Noting.Services
{
    public class ExerciseService
    {
        [Authorize]
        public Task SaveFromText(string rawText, ObjectId userId) 
        {
            var exercise = new Exercise
            {
                RawText = rawText.Trim(),
                Date = DateTime.UtcNow,
                UserId = userId,
                Weight = 0,
                NameTag = "Default",
                Reps = new List<RepEntry>(),
                Notes = string.Empty
            };
            var pattern = @"^(?<weight>\d+(?:\.\d+)?)(?:kg|lb)?\s+(?<exercise>\w+)\s+(?<sets>\d+)x(?<reps>\d+)$";
            var m = Regex.Match(exercise.RawText, pattern, RegexOptions.IgnoreCase);

            exercise.Weight = (int)Math.Round(double.Parse(m.Groups["weight"].Value));
            exercise.NameTag = m.Groups["exercise"].Value;

            int sets = int.Parse(m.Groups["sets"].Value);
            int reps = int.Parse(m.Groups["reps"].Value);
            exercise.Reps = Enumerable
                .Range(0, sets)
                .Select(_ => new RepEntry { Count = reps })
                .ToList();

            DatabaseManipulator.Save(exercise);

            return Task.CompletedTask;

        }
    }
}
