using System;
using System.Collections.Generic;
using System.Linq;
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
            Query = Query.Replace("(", "").Replace(")", "");
            return this;
        }
    }

    class PrintingVisitor<T> : ExpressionVisitor
    {
        public string query;
        private ParameterExpression arg;
        private List<(string prop, string attr)> jsonProperties = new List<(string, string)>();

        public PrintingVisitor(Expression<Func<T, bool>> exp)
        {
            arg = exp.Parameters[0];

            query = exp.Body.ToString();
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (attr != null)
                {
                    Console.WriteLine($"{prop.Name} : {attr.Name}");
                    jsonProperties.Add((prop.Name, attr.Name));
                }
            }

        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Console.WriteLine("Visiting Method Call {0}", node);
            Console.WriteLine(node.Method.Name);

            if (node.ToString().Contains(".Length"))
                throw new InvalidOperationException("use o .Length is not allowed!");

            switch (node.Method.Name)
            {
                case "Contains":
                    Console.WriteLine(" replace with LIKE");
                    query = query.Replace(".Contains(", " LIKE ");
                    break;
                case "StartsWith":
                    query = query.Replace(".StartsWith(", " STARTSWITH ");
                    Console.WriteLine(" replace with StartWith");
                    break;
                case "EndsWith":
                    query = query.Replace(".EndsWith(", " ENDSWITH ");
                    Console.WriteLine(" replace with EndsWith");
                    break;
                case "IsNullOrEmpty":
                    query = query.Replace($"Not(IsNullOrEmpty({node.Arguments[0]}))", $"{node.Arguments[0]}ISNOTEMPTY");
                    query = query.Replace($"IsNullOrEmpty({node.Arguments[0]})", $"{node.Arguments[0]}ISEMPTY");
                    Console.WriteLine(" replace with ISEMPTY");
                    break;
                default:
                    Console.WriteLine("Throw Error here????");
                    break;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Console.WriteLine("Visiting Conditional {0}", node);

            // Recurse down to see if we can simplify...
            var expression = Visit(node.Test);

            // IF(something, then this, or that)
            if (expression is ConstantExpression)
            {
                var container = (bool)((ConstantExpression)expression).Value;
                query = query.Split(",")[container ? 1 : 2];
            }

            return base.VisitConditional(node);
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            Console.WriteLine("Visiting Constant: {0} = {1}", node, node.Value);
            if (!node.ToString().Contains("value("))
                query = query.Replace(node.ToString(), node.Value.ToString());

            query = query.Replace("Guid.Empty", "");
            return base.VisitConstant(node);
        }

        //protected override Expression Visit(UnaryExpression node)
        //{

        //}
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Console.WriteLine("Visiting Binary {0}", node);
            var nodeString = node.ToString();
            if (nodeString[0] == '(' && nodeString[nodeString.Length - 1] == ')')
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
                    query = query.Replace("==", "=");
                    break;
                case ExpressionType.NotEqual:
                    break;
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                //TODO: check if works in SNow
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                    Console.WriteLine("Reaplce with > < => ???");
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
                    return Expression.Constant(value);
                }
                if (member is PropertyInfo)
                {
                    object value = ((PropertyInfo)member).GetValue(container, null);
                    Console.WriteLine("Got value 2: {0}", value);
                    return Expression.Constant(value);
                }
            }
            else
            {
                if (node.Expression == arg)
                {
                    var attributeName = jsonProperties.FirstOrDefault(x => x.prop == node.Member.Name);
                    query = query.Replace(node.ToString(), attributeName.attr != null ? attributeName.attr : node.ToString().Split(".")[1]);
                }
            }
            return base.VisitMember(node);
        }

    }
}
