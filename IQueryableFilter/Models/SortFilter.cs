using IQueryableFilter.Enums;

namespace IQueryableFilter.Models
{
    public class SortFilter
    {
        public SortDirection Direction { get; set; } = SortDirection.Ascending;
        public string PropertyName { get; set; } = string.Empty;
    }
}
