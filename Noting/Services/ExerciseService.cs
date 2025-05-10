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

        private readonly ICurrentUserService _currentUser;

        private readonly List<PatternConfig> _cleanupRules;
        private readonly Dictionary<string, Regex> _parsingPatterns;
        private readonly Regex _tokenizer;

        public ExerciseService(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
            _cleanupRules = RegexPatterns.LoadConfigs("regexParsing.json", "Cleanup");
            _parsingPatterns = RegexPatterns.LoadLookup("regexParsing.json", "Parsing");
            _tokenizer = RegexPatterns.LoadLookup("lexerPatterns.json")["Tokenize"];
        }
        [Authorize]
        public async Task<Exercise> SaveFromText(string rawText, DateTimeOffset noteDate, ObjectId? id = null)
        {
            var userId = await _currentUser.GetUserIdAsync();
            if (userId == null)
                throw new UnauthorizedAccessException("User must be logged in to save an exercise.");

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
                UserId = userId.Value,
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
        public async Task<List<Exercise>> GetAllForCurrentUser()
        {
            var userId = await _currentUser.GetUserIdAsync();
            if (userId == null)
                return new List<Exercise>();

            return await DatabaseManipulator
                .Collection<Exercise>()
                .Find(e => e.UserId == userId)
                .SortByDescending(e => e.Date)
                .ToListAsync();
        }
        public async Task<List<Exercise>> GetSameNameExercises()
        {
            var userId = await _currentUser.GetUserIdAsync();
            if (userId == null)
                return new List<Exercise>();

            var collection = DatabaseManipulator.Collection<Exercise>();

            // Step 1: Get one exercise to extract the NameTag
            var firstExercise = await collection
                .Find(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (firstExercise == null)
                return new List<Exercise>();

            string name = firstExercise.NameTag;

            // Step 2: Fetch all exercises with that same name
            return await collection
                .Find(e => e.UserId == userId && e.NameTag == name)
                .ToListAsync();
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

                bool isRepStart = (i + 1 < tokens.Count && tokens[i + 1].Type == "Separator");
                if (exerciseName.Length == 0
                    && weight == null
                    && token.Type == "Number"
                    && !isRepStart
                    && int.TryParse(token.Value, out var leading))
                {
                    weight = leading;
                    continue;
                }

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
                        isNameBuilding = false;
                        break;
                    }
                    // Stuff for Chain that is +2 size
                    if (repNumbers.Count >= 2)
                    {

                        int? nextWeight = null;
                        if (weight == null
                            && j + 1 < tokens.Count
                            && tokens[j + 1].Type == "Number"
                            && int.TryParse(tokens[j + 1].Value, out var w))
                        {
                            nextWeight = w;
                        }

                        if (weight == null)
                        {
                            if (pendingNumbers.Count > 0)
                            {
                                weight = pendingNumbers.Last();
                                pendingNumbers.RemoveAt(pendingNumbers.Count - 1);
                            }
                            else if (nextWeight.HasValue)
                            {
                                weight = nextWeight.Value;
                            }
                        }

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
                            if (IsProbablySetXRep(repNumbers[0], repNumbers[1]))
                            {
                                var (sets, r) = DisambiguateTwoNumbers(repNumbers[0], repNumbers[1]);
                                repsPerSet = Enumerable.Repeat(r, sets).ToList();
                            }
                            else
                            {
                                repsPerSet = repNumbers;
                            }
                        } else
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
                int mn = leftovers.Min();
                int mx = leftovers.Max();

                if (leftovers.Count == 1)
                {
                    var x = leftovers[0];
                    if (x <= MaxRep)
                        repsPerSet.Add(x);
                    else
                        weight = x;
                }
                else if (mn <= 5 && leftovers.Count == 2)
                {
                    var (sets, reps) = DisambiguateTwoNumbers(leftovers[0], leftovers[1]);
                    repsPerSet = Enumerable.Repeat(reps, sets).ToList();
                }
                else
                {


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
        private bool IsProbablySetXRep(int a, int b)
        {
            const int maxSets = 6;
            const int maxReps = 30;

            return (a <= maxSets && b <= maxReps && b > a)
                || (b <= maxSets && a <= maxReps && a > b);
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
