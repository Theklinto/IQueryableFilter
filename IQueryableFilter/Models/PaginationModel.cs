namespace IQueryableFilter.Models
{
    public class PaginationModel<T> where T : class
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; } = 0;
    }
}
