using System.Text.Json.Serialization;
using System.Text.Json;
using IQueryableFilter.Interfaces;

namespace IQueryableFilter.JsonConverters
{
    public class IGenericFilterConverter : JsonConverter<IGenericFilter>
    {
        public override IGenericFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //Copy of reader with start position
            Utf8JsonReader backupReader = reader;

            //Check valid json
            if (reader.TokenType is not JsonTokenType.StartObject)
                throw new JsonException();

            string filterTypeString = string.Empty;
            IGenericFilter? filter = null;

            //Read through all of the reader, otherwise it'll will throw error
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return filter;

                //We are searching for the FilterType to determine what to deserialize to
                if (reader.TokenType != JsonTokenType.PropertyName || filter is not null)
                {
                    reader.Skip();
                    continue;
                }

                //If it's not the FilterType skip
                bool isFilterProp = reader.GetString()?
                    .Equals(nameof(IGenericFilter.FilterType), StringComparison.CurrentCultureIgnoreCase) ?? false;
                if (isFilterProp is false)
                {
                    reader.Skip();
                    continue;
                }

                reader.Read(); //Reads next line
                filterTypeString = reader.GetString() ?? string.Empty; //Get filter type as string
                if (string.IsNullOrWhiteSpace(filterTypeString))
                    throw new JsonException($"Provided filter type was empty.");

                //See if type can be found
                Type? filterType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .FirstOrDefault(x => typeof(IGenericFilter).IsAssignableFrom(x) && x.Name.Equals(filterTypeString, StringComparison.CurrentCultureIgnoreCase))
                    ?? throw new JsonException($"No filter type could be found with the name {filterTypeString}");

                //Decide what type to deserialize as, based on the FilterType
                filter = JsonSerializer.Deserialize(ref backupReader, filterType, IQueryableConfig.JsonSerializerOptions) as IGenericFilter;
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, IGenericFilter value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value);
        }
    }
}
