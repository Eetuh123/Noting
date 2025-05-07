using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Noting.Services;  // adjust if your namespace differs

namespace Noting.Controllers
{
    [Route("chart")]
    public class ChartController : Controller
    {
        private readonly ExerciseService _exerciseService;
        public ChartController(ExerciseService exerciseService)
            => _exerciseService = exerciseService;

        //–– DTO for the single-exercise chart
        public class ChartPoint
        {
            public string Date { get; set; } = "";
            public int Weight { get; set; }
            public int Reps { get; set; }
        }

        //–– Models for the “All” multi-series chart
        public class MetricSeries
        {
            public List<int> Weight { get; set; } = new();
            public List<int> Reps { get; set; } = new();
            public List<int> Volume { get; set; } = new();
            public List<double> OneRm { get; set; } = new();
        }
        public class AllChartViewModel
        {
            public List<string> Labels { get; set; } = new();
            public Dictionary<string, MetricSeries> Raw { get; set; }
                = new Dictionary<string, MetricSeries>();
        }

        [HttpGet("{exerciseName}")]
        public async Task<IActionResult> ForExercise(string exerciseName)
        {
            var user = HttpContext.User;
            var all = await _exerciseService.GetAllForCurrentUser(user);

            if (exerciseName.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Build “All” chart
                var dates = all
                    .Select(e => e.Date.ToString("dd/MM"))
                    .Distinct()
                    .OrderBy(d => DateTime.ParseExact(d, "dd/MM", CultureInfo.InvariantCulture))
                    .ToList();

                var raw = all
                  .GroupBy(e => e.NameTag, StringComparer.OrdinalIgnoreCase)
                  .ToDictionary(
                    g => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key),
                    g =>
                    {
                        var byDate = g
                          .GroupBy(x => x.Date.ToString("dd/MM"))
                          .ToDictionary(
                            dg => dg.Key,
                            dg => new {
                                Weight = dg.Last().Weight,
                                Reps = dg.Sum(x => x.Reps.Sum(r => r.Reps.GetValueOrDefault()))
                            });

                        var weights = dates.Select(d => byDate.ContainsKey(d) ? byDate[d].Weight : 0).ToList();
                        var reps = dates.Select(d => byDate.ContainsKey(d) ? byDate[d].Reps : 0).ToList();
                        var volume = weights.Zip(reps, (w, r) => w * r).ToList();
                        var oneRm = weights.Zip(reps, (w, r) => w * (1 + r / 30.0)).ToList();

                        return new MetricSeries
                        {
                            Weight = weights,
                            Reps = reps,
                            Volume = volume,
                            OneRm = oneRm
                        };
                    });

                var vm = new AllChartViewModel { Labels = dates, Raw = raw };
                return View("ChartAll", vm);
            }

            // Build single-exercise chart
            var data = all
              .Where(e => e.NameTag.Equals(exerciseName, StringComparison.OrdinalIgnoreCase))
              .GroupBy(e => e.Date.Date)
              .OrderBy(g => g.Key)
              .Select(g => new ChartPoint
              {
                  Date = g.Key.ToString("dd/MM"),
                  Weight = g.Last().Weight,
                  Reps = g.Sum(e => e.Reps.Sum(r => r.Reps.GetValueOrDefault()))
              })
              .ToList();

            return View("Chart", data);
        }
    }
}
