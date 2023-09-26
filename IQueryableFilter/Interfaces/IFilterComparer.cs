using IQueryableFilter.JsonConverters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IQueryableFilter.Interfaces
{
    [JsonConverter(typeof(IFilterComparerConverter))]
    public interface IFilterComparer
    {
        public int Identifier { get; }
    }
}
