using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Net
{
    public class GraphQLSchema<TContext> where TContext : IDisposable, new()
    {
        private readonly List<GraphQLType> _types;
        private readonly List<GraphQLQueryBase<TContext>> _queries = new List<GraphQLQueryBase<TContext>>();

        public GraphQLSchema()
        {
            _types = LoadSchema();
        }

        public void CreateQuery<TArgs, TEntity>(string name, TArgs argObj, Func<TContext, TArgs, IQueryable<TEntity>> queryableGetter, bool list = false)
        {
            CreateQuery(name, queryableGetter, list);
        }

        public void CreateQuery<TArgs, TEntity>(string name, Func<TContext, TArgs, IQueryable<TEntity>> queryableGetter, bool list = false)
        {
            var args = typeof(TArgs)
                .GetProperties()
                .Select(p => new InputValue
                {
                    Name = p.Name,
                    Type = GetGQLType(p.PropertyType)
                }).ToList();
            _queries.Add(new GraphQLQuery<TContext, TArgs, TEntity>
            {
                Name = name,
                Type = GetGQLType(typeof(TEntity)),
                Arguments = args,
                GetQueryable = queryableGetter,
                List = list
            });
        }

        public void CreateQuery<TEntity>(string name, Func<TContext, IQueryable<TEntity>> queryableGetter, bool list = false)
        {
            CreateQuery(name, new object(), (db, args) => queryableGetter(db), list);
        }

        public GraphQLQueryBase<TContext> FindQuery(string name)
        {
            // TODO: Name duplicates?
            return _queries.FirstOrDefault(q => q.Name == name);
        }

        private GraphQLType GetGQLType(Type type)
        {
            return GetGQLType(type, _types);
        }

        private static GraphQLType GetGQLType(Type type, List<GraphQLType> types)
        {
            return types.First(t => t.CLRType == type);
        }

        private static List<GraphQLType> LoadSchema()
        {
            var types = typeof(TContext)
                .GetProperties()
                .Where(p => p.PropertyType.IsGenericType && TypeHelpers.IsAssignableToGenericType(p.PropertyType, typeof(IQueryable<>)))
                .Select(p => new GraphQLType(p.PropertyType.GetGenericArguments()[0]))
                .Concat(GetPrimitives())
                .ToList();

            // TODO: PERF: Dictionary?
            foreach (var type in types.Where(t => !t.IsScalar))
            {
                var props = type.CLRType.GetProperties();
                foreach (var prop in props)
                {
                    var propType = prop.PropertyType;
                    if (TypeHelpers.IsAssignableToGenericType(propType, typeof(ICollection<>)))
                        propType = propType.GetGenericArguments()[0];
                    var gqlType = GetGQLType(propType, types);
                    var field = new GraphQLField
                    {
                        Name = prop.Name.ToCamelCase(),
                        Type = gqlType,
                        PropInfo = prop,
                    };
                    type.Fields.Add(field);
                }
            }

            return types;
        }

        private static IEnumerable<GraphQLType> GetPrimitives()
        {
            return new[]
            {
                new GraphQLType(typeof(int)) { IsScalar = true },
                new GraphQLType(typeof(float)) { IsScalar = true },
                new GraphQLType(typeof(string)) { IsScalar = true },
                new GraphQLType(typeof(bool)) { IsScalar = true },
                new GraphQLType(typeof(Guid)) { Name = "ID", IsScalar = true }
            };
        }
    }

    public class GraphQLType
    {
        public GraphQLType(Type type)
        {
            CLRType = type;
            Name = type.Name;
            Fields = new List<GraphQLField>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<GraphQLField> Fields { get; set; }
        public Type CLRType { get; set; }
        public bool IsScalar { get; set; } // TODO: TypeKind?
    }

    public class GraphQLField
    {
        public GraphQLField()
        {
            Arguments = new List<InputValue>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public PropertyInfo PropInfo { get; set; }
        public GraphQLType Type { get; set; }
        public List<InputValue> Arguments { get; set; }
    }

    public class InputValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GraphQLType Type { get; set; }
    }
}
