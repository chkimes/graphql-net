using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static TArgs GetArgs<TArgs>(List<Input> inputs)
        {
            var paramlessCtor = typeof(TArgs).GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return GetParamlessArgs<TArgs>(paramlessCtor, inputs);
            var anonTypeCtor = typeof(TArgs).GetConstructors().Single();
            return GetAnonymousArgs<TArgs>(anonTypeCtor, inputs);
        }

        private static TArgs GetAnonymousArgs<TArgs>(ConstructorInfo anonTypeCtor, List<Input> inputs)
        {
            var parameters = anonTypeCtor
                .GetParameters()
                .Select(p => GetParameter(p, inputs))
                .ToArray();
            return (TArgs)anonTypeCtor.Invoke(parameters);
        }

        private static object GetParameter(ParameterInfo param, List<Input> inputs)
        {
            var input = inputs.FirstOrDefault(i => i.Name == param.Name);
            return input != null ? input.Value : TypeHelpers.GetDefault(param.ParameterType);
        }

        private static TArgs GetParamlessArgs<TArgs>(ConstructorInfo paramlessCtor, List<Input> inputs)
        {
            var args = (TArgs)paramlessCtor.Invoke(null);
            foreach (var input in inputs)
                typeof(TArgs).GetProperty(input.Name).GetSetMethod().Invoke(args, new[] { input.Value });
            return args;
        }
    }
}
