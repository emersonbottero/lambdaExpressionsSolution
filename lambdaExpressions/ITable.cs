using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace lambdaExpressions
{
    public interface ITable<T>
    {
        public ITable<T> CustomWhere(Expression<Func<T, bool>> expression);
    }
}
