using System.Text.Json;
using IQueryableFilter.JsonConverters;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace IQueryableFilter
{
    internal static class IQueryableConfig
    {
        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            JsonSerializerOptions options = new()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            new List<JsonConverter>
            {
                new IGenericFilterConverter(),
                new IFilterComparerConverter()
            }.ForEach(options.Converters.Add);

            return options;
        }

        private static JsonSerializerOptions _jsonSerializerOptions = GetJsonSerializerOptions();
        public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions;
    }
}
