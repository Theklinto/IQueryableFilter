using IQueryableFilter.Enums;
using IQueryableFilter.Interfaces;

namespace IQueryableFilter.Models
{
    public class QueryFilter
    {
        public List<IGenericFilter> Filters { get; set; } = new();
        public SortFilter SortFilter { get; set; } = new();
        public int Take { get; set; } = 0;
        public int Skip { get; set; } = 0;
        public FilterMode FilterMode { get; set; } = FilterMode.All;
    }
    
    
}
