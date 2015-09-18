using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Net
{
    public class ScratchResolvers
    {
        private static List<LambdaExpression> Expressions = new List<LambdaExpression>();
        public static void AddResolver<TContext, TItem>(string name, Expression<Func<TContext, TItem, object>> expr)
        {
            Expressions.Add(expr as LambdaExpression);
        }

        public static Expression<Func<TContext, IQueryable<GQLQueryObject>>> Resolve<TContext, TItem>(Expression<Func<TContext, IQueryable<TItem>>> baseExpr)
        {
            var dbParam = (baseExpr as LambdaExpression).Parameters[0];
            var itemParam = Expression.Parameter(typeof(TItem), "p");

            var members = new List<Expression>();
            foreach(var expr in Expressions)
            {
                var oldDb = expr.Parameters[0];
                var oldItem = expr.Parameters[1];

                var body = expr.Body;
                var intermediateBody = ParameterReplacer.Replace(body, oldDb, dbParam);
                var finalBody = ParameterReplacer.Replace(intermediateBody, oldItem, itemParam);
                members.Add(finalBody);
            }

            if (false)
                Queryable.Select<TItem, GQLQueryObject>(null, t => new GQLQueryObject());
            var memberInit = GetMemberInit(members);
            var selector = (Expression<Func<TItem, GQLQueryObject>>)Expression.Lambda(memberInit, itemParam);
            var selectorExpr = Expression.Quote(selector);
            //var selectMethod = typeof(Queryable).GetMethods(BindingFlags.Static|BindingFlags.Public).First(m => m.Name == "Select" && m.GetParameters().Count() == 2);
            //var closedGenericSelectMethod = selectMethod.MakeGenericMethod(typeof(TItem), typeof(GQLQueryObject));
            var call = Expression.Call(typeof(Queryable), "Select", new[] { typeof(TItem), typeof(GQLQueryObject)}, baseExpr.Body, selectorExpr);
            //var call = Expression.Call(null, closedGenericSelectMethod, baseExpr.Body, selector);
            return (Expression<Func<TContext, IQueryable<GQLQueryObject>>>)Expression.Lambda(call, dbParam);
        }

        private static MemberInitExpression GetMemberInit(IEnumerable<Expression> maps)
        {
            var bindings = maps.Select((expr, i) => GetBinding(expr, i + 1)).ToList();

            bindings.AddRange(Enumerable.Range(bindings.Count + 1, 20 - bindings.Count).Select(GetEmptyBinding));
            return Expression.MemberInit(Expression.New(typeof(GQLQueryObject)), bindings);
        }

        // Stupid EF limitation
        private static MemberBinding GetEmptyBinding(int n)
        {
            return Expression.Bind(typeof(GQLQueryObject).GetMember($"Field{n}")[0], Expression.Constant(0));
        }

        private static MemberBinding GetBinding(Expression bindToExpr, int n)
        {
            var mapFieldName = $"Field{n}";
            return Expression.Bind(typeof(GQLQueryObject).GetMember(mapFieldName)[0], bindToExpr);
        }

        public static class Context<TContext>
        {
            public static void AddQuery<TArgs, TEntity>(string name, TArgs obj, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> func)
            {
                throw new NotImplementedException();
            }

            public static void AddQuery<TEntity>(string name, Expression<Func<TContext, IQueryable<TEntity>>> expr)
            {
                throw new NotImplementedException();
            }

            public static QueryAdder<TContext, TArgs> UsingParameters<TArgs>(TArgs o)
            {
                return new QueryAdder<TContext, TArgs>();
            }
        }

        public class QueryAdder<TContext, TArgs>
        {
            public void AddQuery<TEntity>(string users, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> expr)
            {
                throw new NotImplementedException();
            }
        }
    }

    public static class ParameterReplacer
    {
        // Produces an expression identical to 'expression'
        // except with 'source' parameter replaced with 'target' expression.     
        public static Expression Replace
                        (Expression expression,
                        ParameterExpression source,
                        Expression target)
        {
            return new ParameterReplacerVisitor(source, target)
                        .Visit(expression);
        }

        private class ParameterReplacerVisitor : ExpressionVisitor
        {
            private ParameterExpression _source;
            private Expression _target;

            public ParameterReplacerVisitor
                    (ParameterExpression source, Expression target)
            {
                _source = source;
                _target = target;
            }

            //internal Expression<TOutput> VisitAndConvert(Expression root)
            //{
            //    return (Expression<TOutput>)VisitLambda(root);
            //}

            //protected override Expression VisitLambda(Expression<T> node)
            //{
            //    // Leave all parameters alone except the one we want to replace.
            //    var parameters = node.Parameters
            //                         .Where(p => p != _source);

            //    return Expression.Lambda<TOutput>(Visit(node.Body), parameters);
            //}

            protected override Expression VisitParameter(ParameterExpression node)
            {
                // Replace the source with the target, visit other params as usual.
                return node == _source ? _target : base.VisitParameter(node);
            }
        }
    }
}
