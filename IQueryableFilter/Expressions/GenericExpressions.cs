using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IQueryableFilter.Expressions
{
    /// <summary>
    /// Contains generic expressions, that should only be used on primitive types. Each Expression will have a list of tested types.
    /// Other types may or may not behave as expected.
    /// </summary>
    public static class GenericExpressions
    {
        /// <summary>
        /// Converts a property value to string, using ToString() implementation. And searchs using Contains method.
        /// <para>
        ///     Approved types: 
        ///         <see cref="string"/> | 
        ///         <see cref="long"/> | 
        ///         <see cref="int"/> | 
        ///         <see cref="double"/> |
        ///         <see cref="DateTime"/>
        /// </para>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="property"></param>
        /// <param name="searchExpression"></param>
        /// <returns></returns>
        public static Expression? StringContainsExpression(ParameterExpression parameter, PropertyInfo property, string searchExpression)
        {
            if (string.IsNullOrWhiteSpace(searchExpression))
                return null;

            //Defined search expression argument and type
            ConstantExpression argument = Expression.Constant(searchExpression, typeof(string));
            //Define the property that is being used, and the entity it's attached to
            MemberExpression exProperty = Expression.Property(parameter, property);

            Expression conversionExpression;
            //EF Can't translate ToString on strings, so we have to typecheck it
            if (property.PropertyType == typeof(string))
                conversionExpression = Expression.PropertyOrField(parameter, property.Name);
            else
                conversionExpression = Expression.Call(exProperty, nameof(ToString), null);

            Expression expr = Expression.Call(conversionExpression, nameof(string.Contains), null, argument);

            return expr;
        }

        public static Expression? GetEqualExpression(ParameterExpression parameter, PropertyInfo property, string searchExpression)
        {
            if (string.IsNullOrWhiteSpace(searchExpression))
                return null;

            //Defined search expression argument and type
            ConstantExpression argument = Expression.Constant(searchExpression, typeof(string));
            //Define the property that is being used, and the entity it's attached to
            MemberExpression exProperty = Expression.Property(parameter, property);

            Expression conversionExpression;
            //EF Can't translate ToString on strings, so we have to typecheck it
            if (property.PropertyType == typeof(string))
                conversionExpression = Expression.PropertyOrField(parameter, property.Name);
            else
                conversionExpression = Expression.Call(exProperty, nameof(ToString), null);

            return Expression.Equal(conversionExpression, argument);
        }

        public static Expression? GetNotEqualExpression(ParameterExpression parameter, PropertyInfo property, string searchExpression)
        {
            if (string.IsNullOrWhiteSpace(searchExpression))
                return null;

            //Defined search expression argument and type
            ConstantExpression argument = Expression.Constant(searchExpression, typeof(string));
            //Define the property that is being used, and the entity it's attached to
            MemberExpression exProperty = Expression.Property(parameter, property);

            Expression conversionExpression;
            //EF Can't translate ToString on strings, so we have to typecheck it
            if (property.PropertyType == typeof(string))
                conversionExpression = Expression.PropertyOrField(parameter, property.Name);
            else
                conversionExpression = Expression.Call(exProperty, nameof(ToString), null);

            return Expression.NotEqual(conversionExpression, argument);
        }
    }
}
