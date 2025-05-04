using System.Text.Json;
using System.Text.RegularExpressions;

namespace Noting.Models
{
    public class RegexPatterns
    {
        public List<PatternConfig> Patterns { get; set; } = new();

        public static Dictionary<string, Regex> LoadLookup(string jsonPath, string typeFilter = null)
        {
            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize<RegexPatterns>(json)
                         ?? new RegexPatterns();

            return config.Patterns
                         .Where(p => typeFilter == null
                                  || p.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                         .ToDictionary(
                             p => p.Name,
                             p => new Regex(p.Pattern, RegexOptions.IgnoreCase)
                         );
        }
        public static List<PatternConfig> LoadConfigs(string jsonPath, string typeFilter = null)
        {
            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize<RegexPatterns>(json)
                         ?? new RegexPatterns();

            return config.Patterns
                         .Where(p => typeFilter == null
                                  || p.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                         .ToList();
        }
    }
        public class PatternConfig
        {
            public string Name { get; set; } = "";
            public string Pattern { get; set; } = "";
            public string Type { get; set; } = "";
            public string Replacement { get; set; } = "";

        }
}

