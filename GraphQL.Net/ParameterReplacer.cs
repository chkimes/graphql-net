using System.Linq.Expressions;

namespace GraphQL.Net
{
    public static class ParameterReplacer
    {
        // Produces an expression identical to 'expression'
        // except with 'toReplace' parameter replaced with 'replaceWith' expression.     
        public static Expression Replace (Expression expression, ParameterExpression toReplace, Expression replaceWith)
            => new ParameterReplacerVisitor(toReplace, replaceWith).Visit(expression);

        private class ParameterReplacerVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly Expression _target;

            public ParameterReplacerVisitor (ParameterExpression source, Expression target)
            {
                _source = source;
                _target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _source ? _target : base.VisitParameter(node);
            }
        }
    }
}