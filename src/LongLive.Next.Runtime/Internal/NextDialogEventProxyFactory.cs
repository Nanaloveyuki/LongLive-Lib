using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LongLive.Next.Runtime.Internal;

internal static class NextDialogEventProxyFactory
{
    private static readonly ModuleBuilder ModuleBuilder;
    private static readonly Dictionary<string, Type> ProxyTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
    private static int _proxyCounter;

    static NextDialogEventProxyFactory()
    {
        var assemblyName = new AssemblyName("LongLive.Next.Runtime.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
    }

    public static object Create(Type dialogEventInterfaceType, INextDialogEventProxyHandler handler)
    {
        if (dialogEventInterfaceType is null)
        {
            throw new ArgumentNullException(nameof(dialogEventInterfaceType));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var proxyType = GetOrCreateProxyType(dialogEventInterfaceType);
        return Activator.CreateInstance(proxyType, handler)!;
    }

    private static Type GetOrCreateProxyType(Type dialogEventInterfaceType)
    {
        var key = dialogEventInterfaceType.AssemblyQualifiedName ?? dialogEventInterfaceType.FullName ?? dialogEventInterfaceType.Name;

        lock (ProxyTypes)
        {
            if (ProxyTypes.TryGetValue(key, out var cachedType))
            {
                return cachedType;
            }

            var createdType = BuildProxyType(dialogEventInterfaceType);
            ProxyTypes[key] = createdType;
            return createdType;
        }
    }

    private static Type BuildProxyType(Type dialogEventInterfaceType)
    {
        var executeMethod = dialogEventInterfaceType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException(dialogEventInterfaceType.FullName, "Execute");

        var parameters = executeMethod.GetParameters();
        if (parameters.Length != 3)
        {
            throw new InvalidOperationException($"Unexpected Execute signature on {dialogEventInterfaceType.FullName}.");
        }

        var typeBuilder = ModuleBuilder.DefineType(
            $"LongLive.Next.Runtime.Generated.NextDialogEventProxy{_proxyCounter++}",
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        typeBuilder.AddInterfaceImplementation(dialogEventInterfaceType);

        var handlerField = typeBuilder.DefineField("_handler", typeof(INextDialogEventProxyHandler), FieldAttributes.Private | FieldAttributes.InitOnly);
        BuildConstructor(typeBuilder, handlerField);
        BuildExecuteMethod(typeBuilder, dialogEventInterfaceType, executeMethod, handlerField, parameters);

        return typeBuilder.CreateTypeInfo()!.AsType();
    }

    private static void BuildConstructor(TypeBuilder typeBuilder, FieldBuilder handlerField)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { typeof(INextDialogEventProxyHandler) });

        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, handlerField);
        il.Emit(OpCodes.Ret);
    }

    private static void BuildExecuteMethod(
        TypeBuilder typeBuilder,
        Type dialogEventInterfaceType,
        MethodInfo interfaceMethod,
        FieldBuilder handlerField,
        ParameterInfo[] parameters)
    {
        var parameterTypes = new[]
        {
            parameters[0].ParameterType,
            parameters[1].ParameterType,
            parameters[2].ParameterType,
        };

        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            interfaceMethod.ReturnType,
            parameterTypes);

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, handlerField);
        EmitLoadObject(il, 1, parameterTypes[0]);
        EmitLoadObject(il, 2, parameterTypes[1]);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Callvirt, typeof(INextDialogEventProxyHandler).GetMethod(nameof(INextDialogEventProxyHandler.Execute))!);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, dialogEventInterfaceType.GetMethod(interfaceMethod.Name, parameterTypes)!);
    }

    private static void EmitLoadObject(ILGenerator il, short argumentIndex, Type argumentType)
    {
        switch (argumentIndex)
        {
            case 1:
                il.Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                il.Emit(OpCodes.Ldarg_2);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(argumentIndex));
        }

        if (argumentType.IsValueType)
        {
            il.Emit(OpCodes.Box, argumentType);
        }
    }
}
