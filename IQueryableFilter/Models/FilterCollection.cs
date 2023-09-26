using IQueryableFilter.Enums;
using IQueryableFilter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IQueryableFilter.Models
{
    public class FilterCollection
    {
        public List<IGenericFilter> Filters { get; set; } = new();
        public FilterMode FilterMode { get; set; } = FilterMode.Undefined;
    }
}
