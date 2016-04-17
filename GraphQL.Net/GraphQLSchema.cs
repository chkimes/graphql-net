using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Net.SchemaAdapters;

namespace GraphQL.Net
{
    public abstract class GraphQLSchema
    {
        internal readonly VariableTypes VariableTypes = new VariableTypes();
        internal abstract GraphQLType GetGQLType(Type type);
    }

    public class GraphQLSchema<TContext> : GraphQLSchema
    {
        internal readonly Func<TContext> ContextCreator;
        private readonly List<GraphQLType> _types = new List<GraphQLType>();
        private readonly List<GraphQLQueryBase<TContext>> _queries = new List<GraphQLQueryBase<TContext>>();
        internal bool Completed;

        public static readonly ParameterExpression DbParam = Expression.Parameter(typeof (TContext), "db");

        public GraphQLSchema(Func<TContext> contextCreator)
        {
            ContextCreator = contextCreator;
            AddDefaultPrimitives();
        }

        public void AddString<T>(Func<string, T> translate, string name = null)
            => VariableTypes.AddType(CustomVariableType.String(translate, name));

        public void AddInteger<T>(Func<long, T> translate, string name = null)
            => VariableTypes.AddType(CustomVariableType.Integer(translate, name));

        public void AddFloat<T>(Func<double, T> translate, string name = null)
            => VariableTypes.AddType(CustomVariableType.Float(translate, name));

        public void AddBoolean<T>(Func<bool, T> translate, string name = null)
            => VariableTypes.AddType(CustomVariableType.Boolean(translate, name));

        private void AddDefaultPrimitives()
        {
            AddString(Guid.Parse);
            AddFloat(d => (float)d, "Float32");
            AddInteger(i => (int)i, "Int");
        }

        public GraphQLTypeBuilder<TContext, TEntity> AddType<TEntity>(string name = null, string description = null)
        {
            var type = typeof (TEntity);
            if (_types.Any(t => t.CLRType == type))
                throw new ArgumentException("Type has already been added");

            var gqlType = new GraphQLType(type) {IsScalar = type.IsPrimitive, Description = description ?? ""};
            if (!string.IsNullOrEmpty(name))
                gqlType.Name = name;
            _types.Add(gqlType);

            return new GraphQLTypeBuilder<TContext, TEntity>(this, gqlType);
        }

        public GraphQLTypeBuilder<TContext, TEntity> GetType<TEntity>()
        {
            var type = _types.FirstOrDefault(t => t.CLRType == typeof (TEntity));
            if (type == null)
                throw new KeyNotFoundException($"Type {typeof(TEntity).FullName} could not be found.");

            return new GraphQLTypeBuilder<TContext, TEntity>(this, type);
        }

        internal Schema<TContext> Adapter { get; private set; }

        public void Complete()
        {
            if (Completed)
                throw new InvalidOperationException("Schema has already been completed.");

            AddDefaultTypes();

            foreach (var type in _types.Where(t => t.QueryType == null))
                CompleteType(type);

            Adapter = new Schema<TContext>(this);
            Completed = true;
        }

        private static void CompleteType(GraphQLType type)
        {
            // validation maybe perform somewhere else
            if (type.IsScalar && type.Fields.Count != 0)
                throw new Exception("Scalar types must not have any fields defined."); // TODO: Schema validation exception?
            if (!type.IsScalar && type.Fields.Count == 0)
                throw new Exception("Non-scalar types must have at least one field defined."); // TODO: Schema validation exception?

            if (type.IsScalar)
            {
                type.QueryType = type.CLRType;
                return;
            }

            var fieldDict = type.Fields.Where(f => !f.IsPost).ToDictionary(f => f.Name, f => f.Type.IsScalar ? f.Type.CLRType : typeof (object));
            type.QueryType = DynamicTypeBuilder.CreateDynamicType(type.Name + Guid.NewGuid(), fieldDict);
        }

        private void AddDefaultTypes()
        {
            AddType<GraphQLSchema<TContext>>("__Schema")
                .AddField("types", (db, s) => s.Types.ToList())
                .AddField("queryType", (db, s) => (GraphQLType) null) // TODO: queryType
                .AddField("mutationType", (db, s) => (GraphQLType) null) // TODO: mutations + mutationType
                .AddField("directives", (db, s) => new List<GraphQLType>()); // TODO: Directives

            AddType<GraphQLType>("__Type")
                .AddField("kind", (db, t) => GetTypeKind(t))
                .AddField(t => t.Name)
                .AddField(t => t.Description)
                .AddField(t => t.Fields) // TODO: includeDeprecated
                .AddField("interfaces", (db, t) => new List<GraphQLType>());

            AddType<GraphQLField>("__Field")
                .AddField(f => f.Name)
                .AddField(f => f.Description)
                //.AddField(f => f.Arguments) // TODO:
                .AddField(f => f.Type)
                .AddField("isDeprecated", (db, f) => false) // TODO: deprecation
                .AddField("deprecationReason", (db, f) => "");

            this.AddQuery("__schema", db => this);
            this.AddQuery("__type", new {name = ""}, (db, args) => _types.AsQueryable().Where(t => t.Name == args.name).First());

            var method = GetType().GetMethod("AddTypeNameField", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var type in _types.Where(t => !t.IsScalar))
            {
                var genMethod = method.MakeGenericMethod(type.CLRType);
                genMethod.Invoke(this, new object[] {type});
            }
        }

        private static string GetTypeKind(GraphQLType type)
        {
            if (type.IsScalar)
                return "SCALAR";
            return "OBJECT";
            // TODO: interface?, union? enum, input_object, list, non_null
        }

        private void AddTypeNameField<TEntity>(GraphQLType type)
        {
            var builder = new GraphQLTypeBuilder<TContext, TEntity>(this, type);
            builder.AddPostField("__typename", () => type.Name);
        }

        // This signature is pretty complicated, but necessarily so.
        // We need to build a function that we can execute against passed in TArgs that
        // will return a base expression for combining with selectors (stored on GraphQLType.Fields)
        // This used to be a Func<TContext, TArgs, IQueryable<TEntity>>, i.e. a function that returned a queryable given a context and arguments.
        // However, this wasn't good enough since we needed to be able to reference the (db) parameter in the expressions.
        // For example, being able to do:
        //     db.Users.Select(u => new {
        //         TotalFriends = db.Friends.Count(f => f.UserId = u.Id)
        //     })
        // This meant that the (db) parameter in db.Friends had to be the same (db) parameter in db.Users
        // The only way to do this is to generate the entire expression, i.e. starting with db.Users...
        // The type of that expression is the same as the type of our original Func, but wrapped in Expression<>, so:
        //    Expression<TQueryFunc> where TQueryFunc = Func<TContext, IQueryable<TEntity>>
        // Since the query will change based on arguments, we need a function to generate the above Expression
        // based on whatever arguments are passed in, so:
        //    Func<TArgs, Expression<TQueryFunc>> where TQueryFunc = Func<TContext, IQueryable<TEntity>>
        internal void AddQueryInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, IQueryable<TEntity>>>> exprGetter, ResolutionType type)
        {
            if (FindQuery(name) != null)
                throw new Exception($"Query named {name} has already been created.");
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                QueryableExprGetter = exprGetter,
                Schema = this,
                ResolutionType = type,
            });
        }

        internal void AddUnmodifiedQueryInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, TEntity>>> exprGetter)
        {
            if (FindQuery(name) != null)
                throw new Exception($"Query named {name} has already been created.");
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                ExprGetter = exprGetter,
                Schema = this,
                ResolutionType = ResolutionType.Unmodified,
            });
        }

        internal GraphQLQueryBase<TContext> FindQuery(string name) => _queries.FirstOrDefault(q => q.Name == name);

        internal override GraphQLType GetGQLType(Type type)
            => _types.FirstOrDefault(t => t.CLRType == type)
            ?? new GraphQLType(type) { IsScalar = true };

        internal IEnumerable<GraphQLQueryBase<TContext>> Queries => _queries;
        internal IEnumerable<GraphQLType> Types => _types;
    }
}
