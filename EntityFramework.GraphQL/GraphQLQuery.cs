using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EntityFramework.GraphQL
{
    public abstract class GraphQLQueryBase<TContext>
    {
        public string Name { get; set; }
        public GraphQLType Type { get; set; }
        public List<InputValue> Arguments { get; set; }
        public bool List { get; set; }
        public abstract IDictionary<string, object> Execute(TContext context, List<Input> inputs);
    }

    public class GraphQLQuery<TContext, TArgs, TEntity> : GraphQLQueryBase<TContext>
    {
        public Func<TContext, TArgs, IQueryable<TEntity>> GetQueryable { get; set; }

        public override IDictionary<string, object> Execute(TContext context, List<Input> inputs)
        {
            var args = GetArgs(inputs);
            object data;
            var queryable = GetQueryable(context, args);
            if (List)
                data = queryable.ToList();
            else
                data = queryable.FirstOrDefault();

            return new Dictionary<string, object>
            {
                {"data", data }
            };
        }

        private TArgs GetArgs(List<Input> inputs)
        {
            var paramlessCtor = typeof(TArgs).GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return GetParamlessArgs(paramlessCtor, inputs);
            var anonTypeCtor = typeof(TArgs).GetConstructors().Single();
            return GetAnonymousArgs(anonTypeCtor, inputs);
        }

        private TArgs GetAnonymousArgs(ConstructorInfo anonTypeCtor, List<Input> inputs)
        {
            var parameters = anonTypeCtor
                .GetParameters()
                .Select(p => GetParameter(p, inputs))
                .ToArray();
            return (TArgs)anonTypeCtor.Invoke(parameters);
        }

        private object GetParameter(ParameterInfo param, List<Input> inputs)
        {
            var input = inputs.FirstOrDefault(i => i.Name == param.Name);
            if (input != null)
                return input.Value;
            return TypeHelpers.GetDefault(param.ParameterType);
        }

        private TArgs GetParamlessArgs(ConstructorInfo paramlessCtor, List<Input> inputs)
        {
            var args = (TArgs)paramlessCtor.Invoke(null);
            foreach (var input in inputs)
                typeof(TArgs).GetProperty(input.Name).GetSetMethod().Invoke(args, new[] { input.Value });
            return args;
        }
    }
}
