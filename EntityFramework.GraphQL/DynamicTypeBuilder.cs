using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EntityFramework.GraphQL
{
    public class DynamicTypeBuilder
    {
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;
        const string assemblyName = "EntityFramework.GraphQL.DynamicObjects";

        public DynamicTypeBuilder()
        {
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(assemblyName + ".dll");
        }

        public Type CreateDynamicType(string name, Dictionary<string, Type> properties)
        {
            var typeBuilder = _moduleBuilder.DefineType(assemblyName + "." + name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit);
            foreach (var prop in properties)
                CreateProperty(typeBuilder, prop.Key, prop.Value);
            return typeBuilder.CreateType();
        }

        private void CreateProperty(TypeBuilder typeBuilder, string name, Type type)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + name.ToLower(), type, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);

            propertyBuilder.SetGetMethod(CreateGetMethod(typeBuilder, fieldBuilder, name, type));
            propertyBuilder.SetSetMethod(CreateSetMethod(typeBuilder, fieldBuilder, name, type));
        }

        const MethodAttributes methodAttrs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        private MethodBuilder CreateGetMethod(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, string name, Type type)
        {
            var methodBuilder = typeBuilder.DefineMethod("get_" + name, methodAttrs, type, Type.EmptyTypes);
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private MethodBuilder CreateSetMethod(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, string name, Type type)
        {
            var methodBuilder = typeBuilder.DefineMethod("set" + name, methodAttrs, null, new[] { type });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }
    }
}
