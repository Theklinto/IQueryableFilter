using IQueryableFilter.Enums;
using IQueryableFilter.Exceptions;
using IQueryableFilter.Interfaces;
using IQueryableFilter.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace IQueryableFilter.Extensions
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
            Expression<Func<T, bool>>? filterExpression = AssembleQueryFilter<T>(filter, cancellationToken);

            if (filterExpression is null)
                return query;
            else
                return query.Where(filterExpression);

        }

        internal static Expression<Func<T, bool>>? AssembleQueryFilter<T>(QueryFilter queryFilter, CancellationToken cancellationToken)
        {
            //If no filtercollections are defined, return the query
            if (queryFilter.FilterCollections.Any() is false)
                return null;

            Type type = typeof(T);

            Expression? combinedFilterExpression = null;
            //Get parameter that is queried on ([x] => x...)
            ParameterExpression parameter = Expression.Parameter(type);

            //Loop through all properties that are specified in the filter
            //wrongly spelled property names will be ignored
            PropertyInfo[] properties = type.GetProperties();

            FilterException? filterException = null;

            foreach (FilterCollection filterCollection in queryFilter.FilterCollections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Expression? filterCollectionExpression = null;

                try
                {
                    filterCollectionExpression = AssembleFilterCollection(filterCollection, parameter, properties, cancellationToken);
                }
                catch (FilterException ex)
                {
                    if (filterException is null)
                        filterException = ex;
                    else
                        filterException.Errors.AddRange(ex.Errors);
                }

                if (filterException is not null)
                    continue;

                if (combinedFilterExpression is null)
                    combinedFilterExpression = filterCollectionExpression;
                else if (combinedFilterExpression is not null && filterCollectionExpression is not null)
                    combinedFilterExpression = queryFilter.FilterMode switch
                    {
                        FilterMode.All => Expression.AndAlso(combinedFilterExpression, filterCollectionExpression),
                        FilterMode.Some => Expression.OrElse(combinedFilterExpression, filterCollectionExpression),
                        _ => combinedFilterExpression
                    };
            }

            if (filterException is not null)
                throw filterException.AssembleException();
            else if (combinedFilterExpression is null)
                return null;
            else
                return Expression.Lambda<Func<T, bool>>(combinedFilterExpression, parameter);
        }

        internal static Expression? AssembleFilterCollection(FilterCollection filterCollection, ParameterExpression parameter, PropertyInfo[] parameterProperties, CancellationToken cancellationToken)
        {
            FilterException? filterException = null;

            Expression? filterCollectionExpression = null;
            foreach (IGenericFilter filter in filterCollection.Filters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Expression? filterExpression = null;
                try
                {
                    filterExpression = GenerateFilterExpression(filter, parameter, parameterProperties);
                }
                catch (FilterException ex)
                {
                    if (filterException is null)
                        filterException = ex;
                    else
                        filterException.Errors.AddRange(ex.Errors);
                }

                if (filterException is not null)
                    continue;

                if (filterCollectionExpression is null)
                    filterCollectionExpression = filterExpression;
                else if (filterCollectionExpression is not null && filterExpression is not null)
                    filterCollectionExpression = filterCollection.FilterMode switch
                    {
                        FilterMode.All => Expression.AndAlso(filterCollectionExpression, filterExpression),
                        FilterMode.Some => Expression.OrElse(filterCollectionExpression, filterExpression),
                        _ => filterCollectionExpression
                    };
            }

            if (filterException is not null)
                throw filterException;

            return filterCollectionExpression;
        }

        internal static Expression? GenerateFilterExpression(IGenericFilter filter, ParameterExpression parameter, PropertyInfo[] parameterProperties)
        {
            List<string> errors = new();

            //Get propertyInfo
            PropertyInfo? property = parameterProperties.FirstOrDefault(x => x.Name
                .Equals(filter.PropertyName, StringComparison.CurrentCultureIgnoreCase));

            if (property is null || parameter is null || parameterProperties?.Any() is false)
                return null;

            //Errors will be all be collected and returned as one, instead of the first hit
            //Get underlaying type to use in error messages
            Type underlayingFilter = filter.GetType();

            if (filter.AllowPropertyType(property.PropertyType) is false)
                errors.Add($"({underlayingFilter.Name}) The filter does not allow ussage on type {property.PropertyType}");

            //If property doesnt allow the requested comparison type return error
            if (filter.AllowFilterComparer(filter.Comparer) is false)
                errors.Add($"({underlayingFilter.Name}) The filter does not allow the ussage of the provided comparer");

            if (errors.Any())
                throw new FilterException(errors: errors);

            //Return the expression
            return filter.GetExpression(parameter, property);
        }
    }
}
