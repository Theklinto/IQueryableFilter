namespace IQueryableFilter.Exceptions
{
    public class FilterException : Exception
    {
        public List<string> Errors { get; init; }
        public FilterException(string? message = null, List<string>? errors = null, Exception? innerException = null) : base(message, innerException)
        {
            Errors = errors ?? new();
        }

        public FilterException AssembleException() 
            => new(string.Join(Environment.NewLine, Errors), Errors);
    }
}
