using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;
using Noting.Models;
using System.Globalization;
using System.Numerics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public Task SaveFromText(string rawText, ObjectId userId)
        {
            var cleanText = rawText.ToLowerInvariant();

            foreach (var rule in _cleanupRules)
            {

                cleanText = new Regex(rule.Pattern, RegexOptions.IgnoreCase)
                                .Replace(cleanText, rule.Replacement);
            }

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
                RawText = rawText.Trim(),
                Date = DateTime.UtcNow,
                UserId = userId,
                NameTag = parsed.Name,
                Weight = parsed.Weight,
                Reps = parsed.Reps.Select(r => new RepEntry(r)).ToList(),
                Notes = parsed.Notes
            };

            DatabaseManipulator.Save(exercise);

            return Task.CompletedTask;

        }
        public ParsedExercise ParseTokens(List<Token> tokens)
        {
            string exerciseName = "";
            string notes = "";
            int? weight = null;
            List<int> repsPerSet = new();

            var pendingNumbers = new Queue<int>();
            bool nameOpen = true;
            bool isExpectingReps = false;

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case "Number":
                        if (int.TryParse(token.Value, out var num))
                        {
                            if (nameOpen && exerciseName.Length > 0)
                                    nameOpen = false;
                            pendingNumbers.Enqueue(num);
                        }
                        else
                            notes += " " + token.Value;
                        break;

                    case "Unit":
                        if (pendingNumbers.Count > 0)
                        {
                            weight = pendingNumbers.Dequeue();
                            pendingNumbers.Clear();
                        }
                        else
                        {

                            notes += " " + token.Value;
                        }
                        break;

                    case "Separator":
                        isExpectingReps = true;
                        nameOpen = false;
                        break;

                    case "Word":
                        if (nameOpen)
                            exerciseName += (exerciseName.Length > 0 ? " " : "") + token.Value;
                        else
                            notes += " " + token.Value;
                        break;

                    default:
                        notes += " " + token.Value;
                        break;
                }

                if (isExpectingReps && pendingNumbers.Count >= 2)
                {
                    var a = pendingNumbers.Dequeue();
                    var b = pendingNumbers.Dequeue();
                    (int sets, int reps) = DisambiguateTwoNumbers(a, b);
                    repsPerSet = Enumerable.Repeat(reps, sets).ToList();
                    isExpectingReps = false;
                    nameOpen = false;
                }
            }

            if (repsPerSet.Count == 0
                && pendingNumbers.Count >= 2
                && pendingNumbers.All(n => n == pendingNumbers.Peek()))
            {

                int repsValue = pendingNumbers.Dequeue();
                int setsCount = 1 + pendingNumbers.Count;
                repsPerSet = Enumerable.Repeat(repsValue, setsCount).ToList();
                pendingNumbers.Clear();
            }

            if (repsPerSet.Count == 0 && pendingNumbers.Count > 0)
            {
                var candidateNumbers = pendingNumbers.ToList();
                const int minRep = 1;
                const int maxRep = 25;

                if (candidateNumbers.Count == 1)
                {
                    if (candidateNumbers[0] <= maxRep)
                        repsPerSet = new List<int> { candidateNumbers[0] };
                    else
                        weight = candidateNumbers[0];
                }

                else
                {
                    var reps = candidateNumbers.Where(n => n >= minRep && n <= maxRep).ToList();
                    var heavy = candidateNumbers.FirstOrDefault(n => n > maxRep);

                    if (reps.Count > 0)
                        repsPerSet = reps;

                    if (weight == null && heavy > 0)
                        weight = heavy;
                }

                pendingNumbers.Clear();
            }

            if (repsPerSet.Count == 0 && pendingNumbers.Count >= 1)
            {
                weight = pendingNumbers.Dequeue();
            }

            return new ParsedExercise
            {
                Name = exerciseName.Trim(),
                Weight = weight ?? 0,
                Reps = repsPerSet,
                Notes = notes.Trim()
            };
        }

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
