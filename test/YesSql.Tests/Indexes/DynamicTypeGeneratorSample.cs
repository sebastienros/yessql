using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using YesSql.Indexes;

namespace YesSql.Tests.Indexes;
public class DynamicTypeGeneratorSample
{
    public static Type GenType(DynamicTypeDef dynamicTypeDef)
    {
        var nameSpace = dynamicTypeDef.NameSpace;
        var indexTypeFullName = dynamicTypeDef.NameSpace + "." + dynamicTypeDef.ClassName;
        var assemblyName = "DynamicTypesAssembly";
        // Create the dynamic assembly
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);

        // Create a dynamic module in the assembly
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(nameSpace);
        // Define the type
        var typeBuilder = moduleBuilder.DefineType(indexTypeFullName, TypeAttributes.Public);

        typeBuilder.SetParent(typeof(MapIndex));

        foreach (var item in dynamicTypeDef.Fields)
        {
            BuildProperties(typeBuilder, item);
        }

        // Create the type
        var dynamicType = typeBuilder.CreateType();
        // Return the dynamic type
        return dynamicType;
    }

    private static PropertyBuilder BuildProperties(TypeBuilder typeBuilder, DynamicField item)
    {
        var propertyName = item.Name;
        var propType = item.FieldType;

        // Define the property
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propType, null);
        var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);

        // Define the getter method
        var getterBuilder = typeBuilder.DefineMethod("get_" + propertyName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propType, Type.EmptyTypes);

        ILGenerator getterIL = getterBuilder.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getterIL.Emit(OpCodes.Ret);


        // Define the setter method
        var setterBuilder = typeBuilder.DefineMethod("set_" + propertyName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { propType });
        // Define the setter method
        var setterIL = setterBuilder.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, fieldBuilder);
        setterIL.Emit(OpCodes.Ret);

        // Set the getter and setter methods for the property
        propertyBuilder.SetGetMethod(getterBuilder);
        propertyBuilder.SetSetMethod(setterBuilder);

        return propertyBuilder;
    }
}

public class DynamicTypeDef
{
    public string NameSpace { get; set; }
    public string ClassName { get; set; }
    public IEnumerable<DynamicField> Fields { get; set; }
}

public class DynamicField
{
    public string Name { get; set; }
    public Type FieldType { get; set; }
}