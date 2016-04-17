using System;
using System.Collections.Generic;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class VariableTypes
    {
        private readonly Dictionary<Type, CustomVariableType> _customByCLRType
            = new Dictionary<Type, CustomVariableType>();

        private readonly Dictionary<string, CustomVariableType> _customByName
            = new Dictionary<string, CustomVariableType>();

        public ISchemaVariableType ResolveVariableTypeByName(string name)
        {
            CustomVariableType custom;
            return _customByName.TryGetValue(name, out custom) ? custom : null;
        }

        /// <summary>
        /// Return the schema variable type used to represent values of type <paramref name="clrType"/>.
        /// </summary>
        /// <param name="clrType"></param>
        /// <returns></returns>
        public CoreVariableType VariableTypeOf(Type clrType)
        {
            CustomVariableType custom;
            if (_customByCLRType.TryGetValue(clrType, out custom))
            {
                return CoreVariableType.NewNamedType(custom);
            }
            return VariableType.GuessFromCLRType(clrType).Type;
        }

        /// <summary>
        /// Get a CLR object of type <paramref name="desiredCLRType"/> from the value <paramref name="graphQLValue"/>.
        /// </summary>
        /// <param name="graphQLValue"></param>
        /// <param name="desiredCLRType"></param>
        /// <returns></returns>
        public object TranslateValue(Value graphQLValue, Type desiredCLRType)
        {
            CustomVariableType custom;
            if (_customByCLRType.TryGetValue(desiredCLRType, out custom))
            {
                return custom.Translate(graphQLValue);
            }
            return Convert.ChangeType(graphQLValue.ToObject(), desiredCLRType);
        }

        public void AddType(CustomVariableType custom)
        {
            if (_customByName.ContainsKey(custom.TypeName))
            {
                throw new Exception("A custom variable type with the same name has already been added.");
            }
            if (_customByCLRType.ContainsKey(custom.CLRType))
            {
                throw new Exception("A custom variable type for the same CLR type has already been added.");
            }
            _customByName.Add(custom.TypeName, custom);
            _customByCLRType.Add(custom.CLRType, custom);
        }
    }
}
