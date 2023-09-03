namespace IQueryableFilter.Exceptions
{
    public class FilterException : Exception
    {
        public FilterException(string? message = null, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
