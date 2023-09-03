using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IQueryableFilter.Interfaces
{
    public interface IFilterComparer : IComparer<IFilterComparer>
    {
        public int Identifier { get; }
        public string FilterComparerName { get; }
    }
}
