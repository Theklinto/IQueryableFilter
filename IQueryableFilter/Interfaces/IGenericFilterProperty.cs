using IQueryableFilter.Enums;
using System.Linq.Expressions;
using System.Reflection;

namespace IQueryableFilter.Interfaces
{
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
