using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;

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
        private readonly List<ExpressionOptions> _expressionOptions = new List<ExpressionOptions>();
        internal bool Completed;

        public static readonly ParameterExpression DbParam = Expression.Parameter(typeof(TContext), "db");

        public GraphQLSchema(Func<TContext> contextCreator)
        {
            ContextCreator = contextCreator;
            AddType<TContext>("queryType");
            AddDefaultExpressionOptions();
        }

        public void AddEnum<TEnum>(string name = null, string prefix = null) where TEnum : struct // wish we could do where TEnum : Enum
            => VariableTypes.AddType(_ => TypeHandler.Enum<TEnum>(name ?? typeof(TEnum).Name, prefix ?? ""));

        public void AddScalar<TRepr, TOutput>(TRepr shape, Func<TRepr, bool> validate, Func<TRepr, TOutput> translate, string name = null)
            => VariableTypes.AddType(t => TypeHandler.Translate
                (t
                , name ?? typeof(TOutput).Name
                , validate
                , translate
                ));

        public void AddScalar<TRepr, TOutput>(TRepr shape, Func<TRepr, TOutput> translate, string name = null)
            => AddScalar
                (shape, r =>
                {
                    try
                    {
                        translate(r);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }, translate, name);

        public GraphQLTypeBuilder<TContext, TEntity> AddType<TEntity>(string name = null, string description = null)
        {
            var type = typeof(TEntity);
            if (_types.Any(t => t.CLRType == type))
                throw new ArgumentException("Type has already been added");

            var gqlType = new GraphQLType(type) { IsScalar = type.IsPrimitive, Description = description ?? "" };
            if (!string.IsNullOrEmpty(name))
                gqlType.Name = name;
            _types.Add(gqlType);

            return new GraphQLTypeBuilder<TContext, TEntity>(this, gqlType);
        }

        public GraphQLTypeBuilder<TContext, TEntity> GetType<TEntity>()
        {
            var type = _types.FirstOrDefault(t => t.CLRType == typeof(TEntity));
            if (type == null)
                throw new KeyNotFoundException($"Type {typeof(TEntity).FullName} could not be found.");

            return new GraphQLTypeBuilder<TContext, TEntity>(this, type);
        }

        public GraphQLSchema<TContext> WithExpressionOptions(Func<Type, bool> validForQueryType, bool castAssignment = true, bool nullCheckLists = false)
        {
            _expressionOptions.Insert(0, new ExpressionOptions(validForQueryType, castAssignment, nullCheckLists));
            return this;
        }

        internal ExpressionOptions GetOptionsForQueryable(Type queryType)
            => _expressionOptions.FirstOrDefault(o => o.ValidForQueryType(queryType));

        private void AddDefaultExpressionOptions()
        {
            // these will execute in reverse order

            // Default
            WithExpressionOptions(t => true, castAssignment: true, nullCheckLists: false);

            // In-memory against IEnumerable or Schema (introspection)
            WithExpressionOptions(t => t.FullName.StartsWith("System.Linq.EnumerableQuery")
                                    || t.FullName.StartsWith("GraphQL.Parser"), castAssignment: true, nullCheckLists: true);

            // Entity Framework
            WithExpressionOptions(t => t.FullName.StartsWith("System.Data.Entity.Infrastructure.DbQuery"), castAssignment: false, nullCheckLists: false);
        }

        internal Schema<TContext> Adapter { get; private set; }

        public void Complete()
        {
            if (Completed)
                throw new InvalidOperationException("Schema has already been completed.");

            AddDefaultTypes();

            VariableTypes.Complete();

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

            // Find the field of the type and all included types
            var fields = type.Fields.Concat(type.IncludedTypes.SelectMany(t => t.Fields));
            var fieldDict = fields.Where(f => !f.IsPost).ToDictionary(f => f.Name, f => f.Type.IsScalar ? TypeHelpers.MakeNullable(f.Type.CLRType) : typeof(object));
            type.QueryType = DynamicTypeBuilder.CreateDynamicType(type.Name + Guid.NewGuid(), fieldDict);
        }

        private void AddDefaultTypes()
        {
            AddEnum<TypeKind>("__TypeKind");
            AddEnum<DirectiveLocation>("__DirectiveLocation");
            var ischema = AddType<IntroSchema>("__Schema");
            ischema.AddListField("types", s => s.Types);
            ischema.AddField("queryType", s => s.QueryType);
            ischema.AddField("mutationType", s => s.MutationType.OrDefault());
            ischema.AddListField("directives", s => s.Directives);

            var itype = AddType<IntroType>("__Type");
            itype.AddField("kind", t => t.Kind);
            itype.AddField("name", t => t.Name.OrDefault());
            itype.AddField("description", t => t.Description.OrDefault());
            // TODO: support includeDeprecated filter argument
            itype.AddListField("fields", t => t.Fields.OrDefault());
            itype.AddListField("inputFields", t => t.InputFields.OrDefault());
            itype.AddField("ofType", s => s.OfType.OrDefault());
            itype.AddListField("interfaces", s => s.Interfaces.OrDefault());
            itype.AddListField("possibleTypes", s => s.PossibleTypes.OrDefault());

            var ifield = AddType<IntroField>("__Field");

            ifield.AddField("name", f => f.Name);
            ifield.AddField("description", f => f.Description.OrDefault());
            ifield.AddListField("args", f => f.Args);
            ifield.AddField("type", f => f.Type);
            ifield.AddField("isDeprecated", f => f.IsDeprecated);
            ifield.AddField("deprecationReason", f => f.DeprecationReason.OrDefault());

            var ivalue = AddType<IntroInputValue>("__InputValue");
            ivalue.AddField("name", v => v.Name);
            ivalue.AddField("description", v => v.Description.OrDefault());
            ivalue.AddField("type", v => v.Type);
            ivalue.AddField("defaultValue", v => v.DefaultValue.OrDefault());

            var ienumValue = AddType<IntroEnumValue>("__EnumValue");

            ienumValue.AddField("name", e => e.Name);
            ienumValue.AddField("description", e => e.Description.OrDefault());
            ienumValue.AddField("isDeprecated", e => e.IsDeprecated);
            ienumValue.AddField("deprecationReason", e => e.DeprecationReason.OrDefault());

            var idirective = AddType<IntroDirective>("__Directive");

            idirective.AddField("name", d => d.Name);
            idirective.AddField("description", d => d.Description.OrDefault());
            idirective.AddListField("locations", d => d.Locations);
            idirective.AddListField("args", d => d.Args);

            this.AddField("__schema", _ => IntroSchema.Of(Adapter));
            this.AddField("__type", new { name = "" },
                (_, args) => IntroSchema.Of(Adapter).Types
                    .FirstOrDefault(t => t.Name.OrDefault() == args.name));

            var method = GetType().GetMethod("AddTypeNameField", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var type in _types.Where(t => !t.IsScalar))
            {
                var genMethod = method.MakeGenericMethod(type.CLRType);
                genMethod.Invoke(this, new object[] { type });
            }
        }

        private void AddTypeNameField<TEntity>(GraphQLType type)
        {
            var builder = new GraphQLTypeBuilder<TContext, TEntity>(this, type);
            if (!type.IncludedTypes.Any())
            {
                // No included types, type name is constant.
                builder.AddPostField("__typename", () => type.Name);
            }
            else
            {
                var param = Expression.Parameter(typeof(TEntity));

                // Generate a nested if-else-expression with all included types.
                // Add the base type name as the last else-expression
                Expression elseExpr = Expression.Constant(type.CLRType.Name);

                // Add type checks for all included types#
                foreach (var includedType in type.IncludedTypes)
                {
                    var testExpr = Expression.TypeIs(param, includedType.CLRType);
                    var expr = Expression.Constant(includedType.CLRType.Name);
                    elseExpr = Expression.Condition(testExpr, expr, elseExpr);
                }
                
                //var lambda = Expression.Lambda<Func<TEntity, string>>(Expression.Block(exprs), param);
                var lambda = Expression.Lambda<Func<TEntity, string>>(elseExpr, param);

                builder.AddField<string>("__typename", lambda);
            }
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
        internal GraphQLFieldBuilder<TContext, TEntity> AddFieldInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, TContext, IEnumerable<TEntity>>>> exprGetter, ResolutionType type)
        {
            if (FindField(name) != null)
                throw new Exception($"Field named {name} has already been created.");
            return GetType<TContext>()
                .AddListFieldInternal(name, exprGetter)
                .WithResolutionType(type);
        }

        internal GraphQLFieldBuilder<TContext, TEntity> AddUnmodifiedFieldInternal<TArgs, TEntity>(string name, Func<TArgs, Expression<Func<TContext, TContext, TEntity>>> exprGetter)
        {
            if (FindField(name) != null)
                throw new Exception($"Field named {name} has already been created.");
            return GetType<TContext>()
                .AddFieldInternal(name, exprGetter)
                .WithResolutionType(ResolutionType.Unmodified);
        }

        internal GraphQLFieldBuilder<TContext, TEntity> AddMutationInternal<TArgs, TEntity, TMutReturn>(string name, Func<TArgs, TMutReturn, Expression<Func<TContext, TContext, IEnumerable<TEntity>>>> exprGetter, ResolutionType type, Func<TContext, TArgs, TMutReturn> mutation)
        {
            if (FindField(name) != null)
                throw new Exception($"Field named {name} has already been created.");
            return GetType<TContext>()
                .AddListMutationInternal(name, exprGetter, mutation)
                .WithResolutionType(type);
        }

        internal GraphQLFieldBuilder<TContext, TEntity> AddUnmodifiedMutationInternal<TArgs, TEntity, TMutReturn>(string name, Func<TArgs, TMutReturn, Expression<Func<TContext, TContext, TEntity>>> exprGetter, Func<TContext, TArgs, TMutReturn> mutation)
        {
            if (FindField(name) != null)
                throw new Exception($"Field named {name} has already been created.");
            return GetType<TContext>()
                .AddMutationInternal(name, exprGetter, mutation)
                .WithResolutionType(ResolutionType.Unmodified);
        }

        internal GraphQLField FindField(string name) => GetGQLType(typeof(TContext)).Fields.FirstOrDefault(f => f.Name == name);

        internal override GraphQLType GetGQLType(Type type)
            => _types.FirstOrDefault(t => t.CLRType == type)
                ?? new GraphQLType(type) { IsScalar = true };

        internal IEnumerable<GraphQLType> Types => _types;
    }
}
