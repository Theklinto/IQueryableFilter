using IQueryableFilter.Enums;
using IQueryableFilter.Exceptions;
using IQueryableFilter.Interfaces;
using IQueryableFilter.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace IQueryableFilter
{
    public static class IQueryableFilter
    {
        /// <summary>
        /// Used to extract <see cref="PaginationModel{T}"/> from the <paramref name="query"/> using the pagination properties of <paramref name="filter"/>.
        /// The <see cref="IQueryable"/> will not be filtered before the extraction. Use <see cref="Filter{T}(IQueryable{T}, QueryFilter, CancellationToken)"/> beforehand
        /// for the filtering.
        /// </summary>
        /// <typeparam name="T">The class being extracted and sorted upon</typeparam>
        /// <param name="query">The query of which the extraction should happen</param>
        /// <param name="filter">Only the pagination related properties will be used</param>
        /// <param name="cancellationToken">Can be supplied for easy cancellation.</param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public static async Task<PaginationModel<T>> Extract<T>(this IQueryable<T> query, QueryFilter filter, CancellationToken cancellationToken = default) where T : class
        {
            int totalCount = await query.CountAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            //Apply sorting
            ParameterExpression entity = Expression.Parameter(typeof(T));
            MemberExpression parameter = Expression.Property(entity, filter.SortFilter.PropertyName);
            //Since some structs can't be unboxed to objects, we have to manually convert them
            Expression<Func<T, object>> orderKeyExpression =
                Expression.Lambda<Func<T, object>>(
                    Expression.Convert(parameter, typeof(object)), entity);

            query = filter.SortFilter.Direction switch
            {
                SortDirection.Descending => query.OrderByDescending(orderKeyExpression),
                _ or SortDirection.Ascending => query.OrderBy(orderKeyExpression),
            };

            if (filter.Skip > 0)
                query = query.Skip(filter.Skip);
            if (filter.Take > 0)
                query = query.Take(filter.Take);

            List<T> result = await query
                .ToListAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return new PaginationModel<T>
            {
                Data = result,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Used to filter the <paramref name="query"/> based on the <paramref name="filter"/> supplied. All filters will be chained using the 
        /// <see cref="Expression.OrElse(Expression, Expression)"/> operation. Meaning it will only require <typeparamref name="T"/> to match one
        /// of the supplied filters.
        /// <para>If <paramref name="filter"/> doesn't contain any filters, or the final expression turns out to be empty. It will return the original query without modifications</para>
        /// </summary>
        /// <typeparam name="T">Taken from the <paramref name="query"/></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="FilterException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, QueryFilter filter, CancellationToken cancellationToken = default)
        {
            //Check if search string is empty
            if (filter.Filters.Any() is false)
                return query;

            Type type = typeof(T);
            List<string> errors = new();

            Expression? expressionBody = null;
            //Get entity that is queried on ([x] => x...)
            ParameterExpression entity = Expression.Parameter(type);
            //Loop through all properties that are specified in the filter
            //wrongly spelled property names will be ignored
            PropertyInfo[] properties = type.GetProperties();
            foreach (IGenericFilter filterProperty in filter.Filters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //Get propertyInfo
                PropertyInfo? property = properties.FirstOrDefault(x => x.Name == filterProperty.PropertyName);
                if (property is null)
                    continue;

                //Errors will be all be collected and returned as one, instead of the first hit
                #region Guard clauses
                // If property does not have the attribute decoration return error
                Type underlayingFilter = filterProperty.GetType();
                if (filterProperty.AllowPropertyType(property.PropertyType) is false)
                {
                    errors.Add($"({underlayingFilter.Name}) The filter does not allow ussage on type {property.PropertyType}");
                    continue;
                }
                //If property doesnt allow the requested comparison type return error
                if (filterProperty.AllowFilterComparer(filterProperty.Comparer) is false)
                {
                    errors.Add($"({underlayingFilter.Name}) The filter does not allow the ussage of {filterProperty.Comparer.FilterComparerName} filter");
                    continue;
                }
                #endregion

                //Build out the expression based on the comparison type
                Expression? chainExpression = filterProperty.GetExpression(entity, property);

                if (expressionBody is null)
                    expressionBody = chainExpression;
                else if (expressionBody is not null && chainExpression is not null)
                    expressionBody = filter.FilterMode switch
                    {
                        FilterMode.All => Expression.AndAlso(expressionBody, chainExpression),
                        FilterMode.Some => Expression.OrElse(expressionBody, chainExpression),
                        _ => expressionBody
                    };
            }

            if (errors.Any())
                throw new FilterException(string.Join(Environment.NewLine, errors));
            else if (expressionBody is null)
                return query;
            else
                return query.Where(Expression.Lambda<Func<T, bool>>(expressionBody, entity));

        }
    }
}
