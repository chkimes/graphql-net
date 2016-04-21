using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parser;

namespace GraphQL.Net
{
    public class VariableTypes
    {
        private readonly List<Func<ITypeHandler, ITypeHandler>> _customHandlers =
            new List<Func<ITypeHandler, ITypeHandler>>();
        private RootTypeHandler _rootTypeHandler;

        private ITypeHandler TypeHandler => _rootTypeHandler;

        public void AddType(Func<ITypeHandler, ITypeHandler> customHandler)
        {
            if (_rootTypeHandler != null) throw new Exception("Can't add types after completing.");
            _customHandlers.Add(customHandler);
        }

        private class MetaTypeHandler : IMetaTypeHandler
        {
            private readonly VariableTypes _variableTypes;

            public MetaTypeHandler(VariableTypes variableTypes)
            {
                _variableTypes = variableTypes;
            }


            public IEnumerable<ITypeHandler> Handlers(ITypeHandler rootHandler)
                => _variableTypes._customHandlers.Select(h => h(rootHandler));
        }

        public void Complete()
        {
            if (_rootTypeHandler != null) throw new Exception("Variable types already complete.");
            _rootTypeHandler = new RootTypeHandler(new MetaTypeHandler(this));
        }

        public CoreVariableType ResolveVariableTypeByName(string name)
            => _rootTypeHandler.ResolveVariableTypeByName(name)?.Value;

        /// <summary>
        /// Return the schema variable type used to represent values of type <paramref name="clrType"/>.
        /// </summary>
        /// <param name="clrType"></param>
        /// <returns></returns>
        public VariableType VariableTypeOf(Type clrType)
            => TypeHandler.GetMapping(clrType)?.Value.VariableType;

        /// <summary>
        /// Get a CLR object of type <paramref name="desiredCLRType"/> from the value <paramref name="graphQLValue"/>.
        /// </summary>
        /// <param name="graphQLValue"></param>
        /// <param name="desiredCLRType"></param>
        /// <returns></returns>
        public object TranslateValue(Value graphQLValue, Type desiredCLRType)
            => TypeHandler.GetMapping(desiredCLRType)?.Value.Translate.Invoke(graphQLValue);
    }
}
