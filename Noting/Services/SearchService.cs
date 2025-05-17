using Noting.Models;
using System.Globalization;

namespace Noting.Services
{
    public static class SearchService
    {
        public static SearchCriteria BuildCriteria(IEnumerable<string> terms)
        {
            var criteria = new SearchCriteria();
            var list = terms.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i].Trim();

                if (t.Equals("after", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < list.Count
                    && TryParseFullDateOrYear(list[i + 1], out var aft))
                {
                    criteria.DateFrom = aft;
                    i++;
                    continue;
                }

                if (t.Equals("before", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < list.Count
                    && TryParseFullDateOrYear(list[i + 1], out var bef))
                {
                    criteria.DateTo = bef;
                    i++;
                    continue;
                }

                if (t.Equals("between", StringComparison.OrdinalIgnoreCase)
                    && i + 2 < list.Count
                    && TryParseFullDateOrYear(list[i + 1], out var b1)
                    && TryParseFullDateOrYear(list[i + 2], out var b2))
                {
                    criteria.DateFrom = b1;
                    criteria.DateTo = b2;
                    i += 2;
                    continue;
                }

                if (TryParseFullDate(t, out var exact))
                {
                    criteria.FullDates.Add(exact);
                    continue;
                }

                if (TryParsePartialDate(t, out var day, out var mon))
                {
                    criteria.PartialDates.Add((day, mon));
                    continue;
                }

                criteria.Tags.Add(t);
            }

            return criteria;
        }
        public static List<Exercise> FilterExercisesByNoteName(
        IEnumerable<Exercise> exercises,
        IEnumerable<WorkoutNote> notes,
        SearchCriteria criteria
    )
        {

            var byText = FilterBy(
                exercises,
                criteria,
                e => e.Date,
                e => e.NameTag,
                e => e.RawText
            );

            var tags = new HashSet<string>(criteria.Tags, StringComparer.OrdinalIgnoreCase);
            var noteExs = notes
              .Where(n => tags.Contains(n.NameTag))
              .SelectMany(n => n.ExerciseIds)
              .ToHashSet();

            var byNote = exercises.Where(e => noteExs.Contains(e.Id));

            return byText
              .Union(byNote)
              .Distinct()
              .ToList();
        }

        public static List<T> FilterBy<T>(
            IEnumerable<T> source,
            SearchCriteria criteria,
            Func<T, DateTimeOffset> dateSelector,
            Func<T, string> nameSelector,
            Func<T, string> textSelector
        )
        {
            var query = source.AsEnumerable();

            foreach (var dt in criteria.FullDates.Distinct())
                query = query.Where(x => dateSelector(x).Date == dt.Date);

            foreach (var (day, mon) in criteria.PartialDates.Distinct())
                query = query.Where(x =>
                    dateSelector(x).Day == day &&
                    dateSelector(x).Month == mon);

            if (criteria.DateFrom.HasValue)
                query = query.Where(x => dateSelector(x).Date >= criteria.DateFrom.Value.Date);
            if (criteria.DateTo.HasValue)
                query = query.Where(x => dateSelector(x).Date <= criteria.DateTo.Value.Date);

            if (criteria.Date.HasValue)
                query = query.Where(x => dateSelector(x).Date == criteria.Date.Value.Date);

            if (!string.IsNullOrWhiteSpace(criteria.Name))
                query = query.Where(x =>
                    nameSelector(x).Contains(criteria.Name, StringComparison.OrdinalIgnoreCase));

            var tags = criteria.Tags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tags.Any())
            {
                var delimiters = new[] { ' ', '\r', '\n', '\t', ',', '.', ';', ':' };
                query = query.Where(x =>
                    tags.Any(tag =>
                        (!string.IsNullOrEmpty(nameSelector(x)) &&
                         nameSelector(x).Contains(tag, StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrEmpty(textSelector(x)) &&
                         (tag.Contains(' ')
                            ? textSelector(x).Contains(tag, StringComparison.OrdinalIgnoreCase)
                            : textSelector(x)
                                  .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                                  .Any(w => w.Equals(tag, StringComparison.OrdinalIgnoreCase))
                         )
                        )
                    )
                );
            }

            return query.ToList();
        }

        private static bool TryParseFullDateOrYear(string s, out DateTime dt)
        {
            if (TryParseFullDate(s, out dt)) return true;
            if (TryParseYear(s, out _, out dt)) return true;
            dt = default;
            return false;
        }

        private static bool TryParseFullDate(string s, out DateTime dt)
        {
            var formats = new[] { "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
            return DateTime.TryParseExact(
                s,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt
            );
        }

        private static bool TryParsePartialDate(string s, out int day, out int month)
        {
            day = month = 0;
            var parts = s.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            return int.TryParse(parts[0], out day)
                && int.TryParse(parts[1], out month);
        }

        private static bool TryParseYear(string s, out int year, out DateTime dt)
        {
            year = 0;
            dt = default;
            if (int.TryParse(s, out var y) && s.Length == 4)
            {
                year = y;
                dt = new DateTime(y, 1, 1);
                return true;
            }
            return false;
        }
    }
}
