using System.Linq.Expressions;

namespace GraphQL.Net
{
    public static class ParameterReplacer
    {
        /// <summary>
        /// Returns an expression with all occurrences of <paramref name="toReplace"/>
        /// replaced by <paramref name="replaceWith"/>.
        /// </summary>
        /// <param name="expression">Base expression</param>
        /// <param name="toReplace">Parameter to be replaced</param>
        /// <param name="replaceWith">Expression to replace the parameter with</param>
        /// <returns></returns>
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