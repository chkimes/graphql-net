using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;
using GraphQL.Parser.CS;

namespace GraphQL.Net.SchemaAdapters
{
    /// <summary>
    /// Wraps a GraphQL query declared at the root of the schema.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    class SchemaQueryField<TContext> : SchemaQueryableFieldCS<Info>
    {
        private readonly Schema _schema;
        private readonly SchemaRootType<TContext> _declaringType;
        private readonly GraphQLQueryBase<TContext> _query;

        public SchemaQueryField(SchemaRootType<TContext> declaringType, GraphQLQueryBase<TContext> query, Schema schema)
        {
            _declaringType = declaringType;
            _query = query;
            _schema = schema;
            Arguments = query.Arguments.ToDictionary(a => a.ArgumentName);
        }

        public override ISchemaQueryType<Info> DeclaringType => _declaringType;
        public override string FieldName => _query.Name;
        public override ISchemaQueryType<Info> QueryableFieldType => _schema.OfType(_query.Type);
        public override Info Info => new Info(_query);
        public override IReadOnlyDictionary<string, ISchemaArgument<Info>> Arguments { get; }
        public override Complexity EstimateComplexity(IEnumerable<ISchemaArgument<Info>> args)
            => _query.Complexity ?? (args.Any(a => a.ArgumentName.Equals("id", StringComparison.OrdinalIgnoreCase))
            ? Complexity.One
            // We probably have a bunch of entities in any given table.
            // Can we give the schema definition the ability to tell us about arguments other than
            // id that introduce good filtering?
            : Complexity.Of(1000, 10000));
    }
}