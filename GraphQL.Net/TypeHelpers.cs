using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser.Execution;

namespace GraphQL.Net
{
    public static class TypeHelpers
    {
        public static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType))
                return true;

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = givenType.BaseType;
            return baseType != null && IsAssignableToGenericType(baseType, genericType);
        }

        /// <summary>
        /// Instantiate an object of <typeparamref name="TArgs"/> given a list of <paramref name="inputs"/>.
        /// This works for objects with parameterless constructors or anonymous types.
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="inputs"></param>
        /// <returns></returns>
        internal static TArgs GetArgs<TArgs>(IEnumerable<ExecArgument<Info>> inputs)
        {
            var paramlessCtor = typeof(TArgs).GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return GetParamlessArgs<TArgs>(paramlessCtor, inputs);
            var anonTypeCtor = typeof(TArgs).GetConstructors().Single();
            return GetAnonymousArgs<TArgs>(anonTypeCtor, inputs);
        }

        private static TArgs GetParamlessArgs<TArgs>(ConstructorInfo paramlessCtor, IEnumerable<ExecArgument<Info>> inputs)
        {
            var args = (TArgs)paramlessCtor.Invoke(null);
            foreach (var input in inputs)
                typeof(TArgs).GetProperty(input.Argument.ArgumentName).GetSetMethod().Invoke(args, new[] { input.Value.ToObject() });
            return args;
        }

        private static TArgs GetAnonymousArgs<TArgs>(ConstructorInfo anonTypeCtor, IEnumerable<ExecArgument<Info>> inputs)
        {
            var parameters = anonTypeCtor
                .GetParameters()
                .Select(p => GetParameter(p, inputs))
                .ToArray();
            return (TArgs)anonTypeCtor.Invoke(parameters);
        }

        private static object GetParameter(ParameterInfo param, IEnumerable<ExecArgument<Info>> inputs)
        {
            var input = inputs.FirstOrDefault(i => i.Argument.ArgumentName == param.Name);
            return input != null ? input.Value.ToObject() : GetDefault(param.ParameterType);
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
