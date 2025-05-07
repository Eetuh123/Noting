using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Driver;
using Noting.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Noting.Services
{
    public class ExerciseService
    {
        private readonly List<PatternConfig> _cleanupRules;
        private readonly Dictionary<string, Regex> _parsingPatterns;
        private readonly Regex _tokenizer;

        public ExerciseService()
        {
            _cleanupRules = RegexPatterns.LoadConfigs("regexParsing.json", "Cleanup");
            _parsingPatterns = RegexPatterns.LoadLookup("regexParsing.json", "Parsing");
            _tokenizer = RegexPatterns.LoadLookup("lexerPatterns.json")["Tokenize"];
        }
        [Authorize]
        public async Task<Exercise> SaveFromText(string rawText, ObjectId userId, DateTimeOffset noteDate, ObjectId? id = null)
        {

            var cleanText = rawText.ToLowerInvariant();
            foreach (var rule in _cleanupRules)
                cleanText = new Regex(rule.Pattern, RegexOptions.IgnoreCase)
                                .Replace(cleanText, rule.Replacement);


            var matches = _tokenizer.Matches(cleanText);
            var tokens = matches
              .Select(m =>
              {
                  var group = m.Groups
                               .Cast<Group>()
                               .First(g => g.Success && g.Name != "0");
                  return new Token { Type = group.Name, Value = m.Value };
              })
              .ToList();
            var parsed = ParseTokens(tokens);

            var exercise = new Exercise
            {
                Id = id ?? ObjectId.GenerateNewId(),
                RawText = rawText.Trim(),
                Date = noteDate,
                UserId = userId,
                NameTag = parsed.Name,
                Weight = parsed.Weight,
                Reps = parsed.Reps.Select(r => new RepEntry(r)).ToList(),
                Notes = parsed.Notes
            };

            await DatabaseManipulator
              .database
              .GetCollection<Exercise>(nameof(Exercise))
              .ReplaceOneAsync(
                 e => e.Id == exercise.Id,
                 exercise,
                 new ReplaceOptions { IsUpsert = true }
              );

            return exercise;
        }
        public async Task DeleteExerciseAsync(ObjectId exerciseId)
        {
            var filter = Builders<Exercise>
                          .Filter
                          .Eq(e => e.Id, exerciseId);


            var col = DatabaseManipulator
                        .database
                        .GetCollection<Exercise>(nameof(Exercise));

            var result = await col.DeleteOneAsync(filter);
        }
        public async Task<List<Exercise>> GetAllForCurrentUser(ClaimsPrincipal user)
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ObjectId.TryParse(userIdString, out var userId))
                return new List<Exercise>();

            return await DatabaseManipulator
                .Collection<Exercise>()
                .Find(e => e.UserId == userId)
                .SortByDescending(e => e.Date)
                .ToListAsync();
        }
        public async Task<Exercise?> GetByTextAndUserAsync(string text, ObjectId userId)
        {
            return await DatabaseManipulator
                .Collection<Exercise>()
                .Find(e => e.UserId == userId && e.RawText == text)
                .FirstOrDefaultAsync();
        }
        public ParsedExercise ParseTokens(List<Token> tokens)
        {
            string exerciseName = "";
            string notes = "";
            int? weight = null;
            var repsPerSet = new List<int>();
            var pendingNumbers = new List<int>();
            bool isNameBuilding = true;

            const int MaxRep = 30;
            const int OutlierThreshold = 10;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                // Try to create number chain as long as there are numbers and separators
                if (token.Type == "Number"
                    && i + 2 < tokens.Count
                    && tokens[i + 1].Type == "Separator")
                {
                    var repNumbers = new List<int>();
                    int j = i;
                    // Chain Creation code
                    while (true)
                    {
                        if (tokens[j].Type != "Number"
                            || !int.TryParse(tokens[j].Value, out var n))
                            break;
                        repNumbers.Add(n);

                        if (j + 2 < tokens.Count
                            && tokens[j + 1].Type == "Separator"
                            && tokens[j + 2].Type == "Number")
                        {
                            j += 2;
                            continue;
                        }
                        break;
                    }
                    // Stuff for Chain that is +2 size
                    if (repNumbers.Count >= 2)
                    {
                        // pop the last pending number as weight
                        if (weight == null && pendingNumbers.Count > 0)
                        {
                            weight = pendingNumbers.Last();
                            pendingNumbers.RemoveAt(pendingNumbers.Count - 1);
                        }

                        // build repsPerSet
                        if (repNumbers.All(x => x == repNumbers[0]))
                        {
                            repsPerSet = Enumerable.Repeat(repNumbers[0], repNumbers.Count).ToList();
                        }
                        else if (repNumbers.Count == 2)
                        {
                            var (sets, r) = DisambiguateTwoNumbers(repNumbers[0], repNumbers[1]);
                            repsPerSet = Enumerable.Repeat(r, sets).ToList();
                        } 
                        else
                        {
                            repsPerSet = repNumbers.ToList();
                        }

                        i = j;  // skip past the chain
                        continue;
                    }
                }

                switch (token.Type)
                {
                    case "Word":
                        if (isNameBuilding)
                        {
                            // flush any queued numbers as reps—but only if the name has already started
                            if (exerciseName.Length > 0 && pendingNumbers.Count >= 2)
                            {
                                repsPerSet.AddRange(pendingNumbers);
                                pendingNumbers.Clear();
                            }
                            exerciseName += (exerciseName.Length > 0 ? " " : "") + token.Value;
                        }
                        else
                        {
                            notes += (notes.Length > 0 ? " " : "") + token.Value;
                        }
                        break;

                    case "Number":
                        if (exerciseName.Length > 0)
                            isNameBuilding = false;
                        if (int.TryParse(token.Value, out var num))
                            pendingNumbers.Add(num);
                        break;

                    case "Unit":
                        if (exerciseName.Length > 0)
                            isNameBuilding = false;
                        // pop the most recent number as weight
                        if (pendingNumbers.Count > 0)
                        {
                            weight = pendingNumbers.Last();
                            pendingNumbers.RemoveAt(pendingNumbers.Count - 1);
                        }
                        break;

                    case "Separator":
                        if (exerciseName.Length > 0)
                            isNameBuilding = false;
                        break;

                    default:
                        notes += (notes.Length > 0 ? " " : "") + token.Value;
                        break;
                }
            }

            // Guessing the imposter with avarages
            if (repsPerSet.Count == 0 && pendingNumbers.Count > 0)
            {
                var leftovers = pendingNumbers;

                if (leftovers.Count == 1)
                {
                    var x = leftovers[0];
                    if (x <= MaxRep)
                        repsPerSet.Add(x);
                    else
                        weight = x;
                }
                else
                {
                    int mn = leftovers.Min();
                    int mx = leftovers.Max();

                    if (weight == null && mx - mn > OutlierThreshold)
                    {
                        // outlier = weight, rest = reps
                        weight = mx;
                        repsPerSet = leftovers.Where(n => n != mx).ToList();
                    }
                    else
                    {
                        // no outlier = all leftovers are reps
                        repsPerSet = new List<int>(leftovers);
                    }
                }
            }

            return new ParsedExercise
            {
                Name = exerciseName.Trim(),
                Weight = weight ?? 0,
                Reps = repsPerSet,
                Notes = notes.Trim()
            };
        }

        // Comparing 2 diffrent values which one meets the condition
        private (int sets, int reps) DisambiguateTwoNumbers(int a, int b)
        {
            const int maxSets = 6;
            const int maxReps = 30;

            if (a <= maxSets && b <= maxReps && b > a)
                return (a, b);
            if (b <= maxSets && a <= maxReps && a > b)
                return (b, a);

            return (a, b);
        }
        public class ParsedExercise
        {
            public string Name { get; set; } = "";
            public int Weight { get; set; }
            public List<int> Reps { get; set; } = new();
            public string Notes { get; set; } = "";
        }
        public class Token
        {
            public string Type { get; set; } = "";
            public string Value { get; set; } = "";
        }
    }
}
