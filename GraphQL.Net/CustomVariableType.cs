using System;
using GraphQL.Parser;

namespace GraphQL.Net
{
    /// <summary>
    /// A named variable type, which is an alias for a primitive type (string, int, float, etc)
    /// with special validation and translation rules.
    /// </summary>
    public class CustomVariableType : ISchemaVariableType
    {
        private readonly Func<Value, bool> _validate;
        public CustomVariableType
            (string typeName, CoreVariableType coreType, Type clrType, Func<Value, bool> validate, Func<Value, object> translate)
        {
            TypeName = typeName;
            CoreType = coreType;
            _validate = validate;
            Translate = translate;
            CLRType = clrType;
        }

        public CustomVariableType
            (string typeName, CoreVariableType coreType, Type clrType, Func<Value, object> translate)
            : this(typeName, coreType, clrType, v =>
            {
                try
                {
                    translate(v);
                    return true;
                }
                catch
                {
                    return false;
                }
            }, translate) { }

        public bool ValidateValue(Value value) => _validate(value);

        /// <summary>
        /// Translate from a GraphQL value of the appropriate core type to a CLR object.
        /// </summary>
        public Func<Value, object> Translate { get; }
        /// <summary>
        /// The name of this custom type.
        /// </summary>
        public string TypeName { get; }
        /// <summary>
        /// The GraphQL variable type these objects are represented by.
        /// </summary>
        public CoreVariableType CoreType { get; }
        /// <summary>
        /// The type of the CLR objects produced by Translate.
        /// </summary>
        public Type CLRType { get; }

        private static CustomVariableType ForType<T>(string name, PrimitiveType primitiveType, Func<Value, object> translate)
            => new CustomVariableType
                (name ?? typeof(T).Name
                    , CoreVariableType.NewPrimitiveType(primitiveType)
                    , typeof(T)
                    , translate);

        public static CustomVariableType String<T>(Func<string, T> translate, string name = null)
            => ForType<T>(name, PrimitiveType.StringType, v => translate(v.GetString()));

        public static CustomVariableType Float<T>(Func<double, T> translate, string name = null)
            => ForType<T>(name, PrimitiveType.FloatType,  v => translate(v.GetFloat()));

        public static CustomVariableType Integer<T>(Func<long, T> translate, string name = null)
            => ForType<T>(name, PrimitiveType.IntType, v => translate(v.GetInteger()));

        public static CustomVariableType Boolean<T>(Func<bool, T> translate, string name = null)
            => ForType<T>(name, PrimitiveType.BooleanType, v => translate(v.GetBoolean()));
    }
}