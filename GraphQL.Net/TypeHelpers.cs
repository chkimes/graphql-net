using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Net.SchemaAdapters;
using GraphQL.Parser;
using GraphQL.Parser.Execution;
using Microsoft.FSharp.Core;

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

        class SchemaArgument : ISchemaArgument<Info>
        {
            public SchemaArgument(string argumentName, CoreVariableType argumentType)
            {
                ArgumentName = argumentName;
                ArgumentType = argumentType;
            }

            public Info Info => null;
            public string ArgumentName { get; }
            public CoreVariableType ArgumentType { get; }
            public FSharpOption<string> Description => null;
        }

        internal static IEnumerable<ISchemaArgument<Info>> GetArgs<TArgs>(VariableTypes variableTypes)
            => GetArgs(variableTypes, typeof(TArgs));
        internal static IEnumerable<ISchemaArgument<Info>> GetArgs(VariableTypes variableTypes, Type argsType)
        {
            var paramlessCtor = argsType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return argsType.GetProperties()
                    .Select(p => new SchemaArgument(p.Name, variableTypes.VariableTypeOf(p.PropertyType)));
            var anonTypeCtor = argsType.GetConstructors().Single();
            return anonTypeCtor.GetParameters()
                .Select(p => new SchemaArgument(p.Name, variableTypes.VariableTypeOf(p.ParameterType)));
        }

        /// <summary>
        /// Instantiate an object of <typeparamref name="TArgs"/> given a list of <paramref name="inputs"/>.
        /// This works for objects with parameterless constructors or anonymous types.
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="variableTypes"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        internal static TArgs GetArgs<TArgs>(VariableTypes variableTypes, IEnumerable<ExecArgument<Info>> inputs)
            => (TArgs) GetArgs(typeof(TArgs), variableTypes, inputs);
        internal static object GetArgs(Type argsType, VariableTypes variableTypes, IEnumerable<ExecArgument<Info>> inputs)
        {
            var paramlessCtor = argsType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
            if (paramlessCtor != null)
                return GetParamlessArgs(argsType, paramlessCtor, variableTypes, inputs);
            var anonTypeCtor = argsType.GetConstructors().Single();
            return GetAnonymousArgs(anonTypeCtor, variableTypes, inputs);
        }

        private static object GetParamlessArgs
            (Type argsType, ConstructorInfo paramlessCtor, VariableTypes variableTypes, IEnumerable<ExecArgument<Info>> inputs)
        {
            var args = paramlessCtor.Invoke(null);
            foreach (var input in inputs)
            {
                var prop = argsType.GetProperty(input.Argument.ArgumentName);
                prop.GetSetMethod()
                    .Invoke(args, new[]
                    {
                        variableTypes.TranslateValue(input.Value, prop.PropertyType)
                    });
            } 
            return args;
        }

        private static object GetAnonymousArgs
            (ConstructorInfo anonTypeCtor, VariableTypes variableTypes, IEnumerable<ExecArgument<Info>> inputs)
        {
            var parameters = anonTypeCtor
                .GetParameters()
                .Select(p => GetParameter(p, variableTypes, inputs))
                .ToArray();
            return anonTypeCtor.Invoke(parameters);
        }

        private static object GetParameter(ParameterInfo param, VariableTypes variableTypes, IEnumerable<ExecArgument<Info>> inputs)
        {
            var input = inputs.FirstOrDefault(i => i.Argument.ArgumentName == param.Name);
            return input != null
                ? variableTypes.TranslateValue(input.Value, param.ParameterType)
                : GetDefault(param.ParameterType);
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
