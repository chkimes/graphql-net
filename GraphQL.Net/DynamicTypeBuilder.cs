using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GraphQL.Net
{
    // Currently unused - keeping it around since we'll at some point need to dynamically create types that have more than 20 fields
    internal static class DynamicTypeBuilder
    {
        private static readonly ModuleBuilder ModuleBuilder;
        const string AssemblyName = "GraphQL.DynamicObjects";

        static DynamicTypeBuilder()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName + ".dll");
        }

        public static Type CreateDynamicType(string name, Dictionary<string, Type> properties, IEnumerable<Type> implementedInterfaces)
        {
            var typeBuilder = ModuleBuilder.DefineType(AssemblyName + "." + name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Serializable | TypeAttributes.BeforeFieldInit);

            foreach (var implementedInterface in implementedInterfaces)
            {
                typeBuilder.AddInterfaceImplementation(implementedInterface);
            }
            
            foreach (var prop in properties)
                CreateProperty(typeBuilder, prop.Key, prop.Value);
            return typeBuilder.CreateType();
        }
        public static Type CreateDynamicInterface(string name, Dictionary<string, Type> properties)
        {
            var typeBuilder = ModuleBuilder.DefineType(AssemblyName + "." + name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            foreach (var prop in properties)
                CreateProperty(typeBuilder, prop.Key, prop.Value, true);
            return typeBuilder.CreateType();
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string name, Type type, bool isAbstract = false)
        {
            FieldBuilder fieldBuilder = null;
            if (!isAbstract) { 
                fieldBuilder = typeBuilder.DefineField("_" + name.ToLower(), type, FieldAttributes.Private);
            }
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);
            propertyBuilder.SetGetMethod(CreateGetMethod(typeBuilder, fieldBuilder, name, type, isAbstract));
            propertyBuilder.SetSetMethod(CreateSetMethod(typeBuilder, fieldBuilder, name, type, isAbstract));
        }

        const MethodAttributes MethodAttrs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.HideBySig;

        private static MethodBuilder CreateGetMethod(TypeBuilder typeBuilder, FieldInfo fieldBuilder, string name,
            Type type, bool isAbstract)
        {
            if (isAbstract)
            {
                return typeBuilder.DefineMethod("get_" + name, MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.Public, type, Type.EmptyTypes);
            }
            
            var methodBuilder = typeBuilder.DefineMethod("get_" + name, MethodAttrs, type, Type.EmptyTypes);
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);
            
            return methodBuilder;
        }

        private static MethodBuilder CreateSetMethod(TypeBuilder typeBuilder, FieldInfo fieldBuilder, string name, Type type, 
            bool isAbstract)
        {
            var attrs = MethodAttrs;
            if (isAbstract)
            {
                attrs = attrs | MethodAttributes.Abstract | MethodAttributes.Virtual;
            }
            var methodBuilder = typeBuilder.DefineMethod("set" + name, attrs, null, new[] { type });
            if (!isAbstract)
            {
                var generator = methodBuilder.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Stfld, fieldBuilder);
                generator.Emit(OpCodes.Ret);
            }
            return methodBuilder;
        }
    }
}
