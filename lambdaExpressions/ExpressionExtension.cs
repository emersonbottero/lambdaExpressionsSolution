using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace lambdaExpressions
{
    internal static class ExpressionExtension
    {
        public static string ToQueryString(this Expression expression)
        {
            if ((BinaryExpression)expression != null)
            {
                var exp = expression as BinaryExpression;
                Console.WriteLine(expression.NodeType);
                return ToQueryString(exp.Left);
            }

            if ((UnaryExpression)expression != null)
                return expression.ToString();

            return "";
        }
    }
}
