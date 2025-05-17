using Noting.Models;
using System;

namespace Noting.Services
{
    public class SearchState : IDisposable
    {
        private SearchCriteria _current = new();
        public event Action OnChange;

        public SearchCriteria Current => _current;

        public void Update(SearchCriteria criteria)
        {
            _current = new SearchCriteria
            {
                Tags = criteria.Tags.ToList(),
                Name = criteria.Name,
                FullDates = criteria.FullDates.ToList(),
                PartialDates = criteria.PartialDates.ToList(),
                DateFrom = criteria.DateFrom,
                DateTo = criteria.DateTo,
                Date = criteria.Date
            };
            OnChange?.Invoke();
        }

        public void Dispose() => OnChange = null;
    }
}