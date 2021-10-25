using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace lambdaExpressions
{
    internal class Table<T> : ITable<T>
    {
        public string Query;
        public ITable<T> CustomWhere(Expression<Func<T, bool>> expr)
        {

            //_query = expr.ToQueryString();
            var visitor = new PrintingVisitor<T>(expr);
            visitor.Visit(expr);
            Query = visitor.query;
            return this;
        }
    }

    class PrintingVisitor<T> : ExpressionVisitor
    {
        public string query;
        private ParameterExpression arg;
        private List<(string prop, string attr)> jsonProperties = new List<(string, string)>();

        public PrintingVisitor( Expression<Func<T, bool>> exp)
        {
            arg = exp.Parameters[0];

            query = exp.Body.ToString();
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if(attr != null)
                {
                    Console.WriteLine($"{prop.Name} : {attr.Name}");
                    jsonProperties.Add((prop.Name, attr.Name));
                }
            }

        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Console.WriteLine("Visiting Method Call {0}", node);

            if (node.ToString().Contains(".Length"))
                throw new InvalidOperationException("use o .Length is not allowed!");

            switch (node.Method.Name)
            {
                case "Contains":
                    Console.WriteLine(" replace with LIKE");
                    break;
                case "StartsWith":
                    Console.WriteLine(" replace with StartWith");
                    break;
                case "EndsWith":
                    Console.WriteLine(" replace with EndsWith");
                    break;
                default:
                    Console.WriteLine("Throw Error here????");
                    break;
            }

            return base.VisitMethodCall(node);
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            Console.WriteLine("Visiting Constant: {0} = {1}", node, node.Value);
            return base.VisitConstant(node);
        }

        protected override Expression VisitBinary(BinaryExpression node) 
        {
            Console.WriteLine("Visiting Binary {0}", node);
            var nodeString = node.ToString();
            if(nodeString[0] == '(' && nodeString[nodeString.Length -1] == ')')
                query = query.Replace(nodeString, nodeString.Substring(1, nodeString.Length - 2));
            switch (node.NodeType)
            {
                #region Replace operator
                case ExpressionType.And:
                    query = query.Replace(nameof(ExpressionType.And), " ^ ");
                    break;
                case ExpressionType.AndAlso:
                    query = query.Replace(nameof(ExpressionType.AndAlso), " ^ ");
                    break;
                case ExpressionType.Or:
                    query = query.Replace(nameof(ExpressionType.Or), " ^OR ");
                    break;
                case ExpressionType.OrElse:
                    query = query.Replace(nameof(ExpressionType.OrElse), " ^OR ");
                    break;
                case ExpressionType.ExclusiveOr:
                    query = query.Replace(nameof(ExpressionType.ExclusiveOr), " ^OR ");
                    break;
                #endregion
                #region Ignore
                case ExpressionType.Equal:
                    Console.WriteLine("Reaplce with =");
                    break;
                case ExpressionType.NotEqual:
                    break;
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                //TODO: check if works in SNow
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                    Console.WriteLine("Check How to handle {0}", node.NodeType);
                    break;
                #endregion
                default:
                    throw new InvalidOperationException($"{node.NodeType} not allowed in table where expression!");
            }
            return base.VisitBinary(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Console.WriteLine("Visiting Member: {0} => is parameter? {1}", node, node.Expression == arg);
            // Recurse down to see if we can simplify...
            var expression = Visit(node.Expression);

            // If we've ended up with a constant, and it's a property or a field,
            // we can simplify ourselves to a constant
            if (expression is ConstantExpression)
            {
                object container = ((ConstantExpression)expression).Value;
                var member = node.Member;
                if (member is FieldInfo)
                {
                    object value = ((FieldInfo)member).GetValue(container);
                    Console.WriteLine("Got value: {0}", value);
                    query = query.Replace((node as Expression).ToString(), value.ToString());
                    //return Expression.Constant(value);
                }
                if (member is PropertyInfo)
                {
                    object value = ((PropertyInfo)member).GetValue(container, null);
                    Console.WriteLine("Got value 2: {0}", value);
                    //return Expression.Constant(value);
                }
            }
            return base.VisitMember(node);
        }

    }
}
