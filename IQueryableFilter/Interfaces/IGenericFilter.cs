using IQueryableFilter.Enums;
using IQueryableFilter.JsonConverters;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace IQueryableFilter.Interfaces
{
    [JsonConverter(typeof(IGenericFilterConverter))]
    public interface IGenericFilter
    {
        public string FilterType { get; init; }
        public string PropertyName { get; init; }
        public IFilterComparer Comparer { get; init; }
        public bool AllowFilterComparer(IFilterComparer comparer);
        public bool AllowPropertyType(Type type);
        public Expression? GetExpression(ParameterExpression parameter, PropertyInfo property);
    }
}
