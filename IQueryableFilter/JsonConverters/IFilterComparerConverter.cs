using IQueryableFilter.Attributes;
using IQueryableFilter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IQueryableFilter.JsonConverters
{
    public class IFilterComparerConverter : JsonConverter<IFilterComparer>
    {
        public override IFilterComparer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader backupReader = reader;
            if (reader.TokenType is not JsonTokenType.StartObject)
                throw new JsonException();

            IFilterComparer? comparer = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return comparer;

                //We are searching for the FilterType to determine what to deserialize to
                if (reader.TokenType != JsonTokenType.PropertyName || comparer is not null)
                {
                    reader.Skip();
                    continue;
                }

                bool isIdentifier = reader.GetString()?
                    .Equals(nameof(IFilterComparer.Identifier), StringComparison.CurrentCultureIgnoreCase) ?? false;
                if (isIdentifier is false)
                {
                    reader.Skip();
                    continue;
                }

                Type? comparerType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(x => x.GetTypes())
                    .FirstOrDefault(x => x.GetCustomAttribute<FilterComparerAttribute>() is not null && 
                        x.IsAssignableTo(typeof(IFilterComparer)))
                    ?? throw new JsonException($"No filter comparer type could be found with the {nameof(FilterComparerAttribute)}");

                reader.Read();
                if (reader.TryGetInt32(out int identifier))
                    comparer = JsonSerializer
                        .Deserialize(ref backupReader, comparerType, IQueryableConfig.JsonSerializerOptions) as IFilterComparer;
            }

            throw new JsonException();
        }


        public override void Write(Utf8JsonWriter writer, IFilterComparer value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value);
    }
}
