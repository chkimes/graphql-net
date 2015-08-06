using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EntityFramework.GraphQL
{
    public static class DynamicTypeBuilder
    {
        private static readonly ModuleBuilder ModuleBuilder;
        const string AssemblyName = "EntityFramework.GraphQL.DynamicObjects";

        static DynamicTypeBuilder()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName + ".dll");
        }

        public static Type CreateDynamicType(string name, Dictionary<string, Type> properties)
        {
            var typeBuilder = ModuleBuilder.DefineType(AssemblyName + "." + name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit);
            foreach (var prop in properties)
                CreateProperty(typeBuilder, prop.Key, prop.Value);
            return typeBuilder.CreateType();
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string name, Type type)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + name.ToLower(), type, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);

            propertyBuilder.SetGetMethod(CreateGetMethod(typeBuilder, fieldBuilder, name, type));
            propertyBuilder.SetSetMethod(CreateSetMethod(typeBuilder, fieldBuilder, name, type));
        }

        const MethodAttributes MethodAttrs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        private static MethodBuilder CreateGetMethod(TypeBuilder typeBuilder, FieldInfo fieldBuilder, string name, Type type)
        {
            var methodBuilder = typeBuilder.DefineMethod("get_" + name, MethodAttrs, type, Type.EmptyTypes);
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private static MethodBuilder CreateSetMethod(TypeBuilder typeBuilder, FieldInfo fieldBuilder, string name, Type type)
        {
            var methodBuilder = typeBuilder.DefineMethod("set" + name, MethodAttrs, null, new[] { type });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            return methodBuilder;
        }
    }
}
