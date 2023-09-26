using IQueryableFilter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IQueryableFilter.Utilities
{
    public static class FilterUtilities
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static QueryFilter? GetQueryFilter(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<QueryFilter>(json, IQueryableConfig.JsonSerializerOptions);
        }
    }
}
