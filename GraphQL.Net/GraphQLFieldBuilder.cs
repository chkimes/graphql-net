﻿using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class GraphQLFieldBuilder<TContext, TExecutionParameters, TField>
    {
        private readonly GraphQLField _field;

        internal GraphQLFieldBuilder(GraphQLField field)
        {
            _field = field;
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> WithDescription(string description)
        {
            _field.Description = description;
            return this;
        }

        public GraphQLFieldBuilder<TContext, TExecutionParameters, TField> WithComplexity(long min, long max)
        {
            _field.Complexity = Complexity.NewRange(Tuple.Create(min, max));
            return this;
        }

        // TODO: This should be removed once we figure out a better way to do it
        internal GraphQLFieldBuilder<TContext, TExecutionParameters, TField> WithResolutionType(ResolutionType type)
        {
            _field.ResolutionType = type;
            return this;
        }
    }
}