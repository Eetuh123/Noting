using System;
using System.Collections.Generic;

namespace Noting.Models
{
    public class SearchCriteria
    {
        public List<string> Tags { get; set; } = new();
        public string Name { get; set; }
        public DateTime? Date { get; set; }
    }
}