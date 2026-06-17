using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LongLive.Next.Runtime.Internal;

internal static class NextDialogEnvQueryProxyFactory
{
    private static readonly ModuleBuilder ModuleBuilder;
    private static readonly Dictionary<string, Type> ProxyTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
    private static int _proxyCounter;

    static NextDialogEnvQueryProxyFactory()
    {
        var assemblyName = new AssemblyName("LongLive.Next.Runtime.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule($"{assemblyName.Name}.Queries");
    }

    public static object Create(Type dialogEnvQueryInterfaceType, INextDialogEnvQueryProxyHandler handler)
    {
        if (dialogEnvQueryInterfaceType is null)
        {
            throw new ArgumentNullException(nameof(dialogEnvQueryInterfaceType));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var proxyType = GetOrCreateProxyType(dialogEnvQueryInterfaceType);
        return Activator.CreateInstance(proxyType, handler)!;
    }

    private static Type GetOrCreateProxyType(Type dialogEnvQueryInterfaceType)
    {
        var key = dialogEnvQueryInterfaceType.AssemblyQualifiedName ?? dialogEnvQueryInterfaceType.FullName ?? dialogEnvQueryInterfaceType.Name;

        lock (ProxyTypes)
        {
            if (ProxyTypes.TryGetValue(key, out var cachedType))
            {
                return cachedType;
            }

            var createdType = BuildProxyType(dialogEnvQueryInterfaceType);
            ProxyTypes[key] = createdType;
            return createdType;
        }
    }

    private static Type BuildProxyType(Type dialogEnvQueryInterfaceType)
    {
        var executeMethod = dialogEnvQueryInterfaceType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException(dialogEnvQueryInterfaceType.FullName, "Execute");

        var parameters = executeMethod.GetParameters();
        if (parameters.Length != 1)
        {
            throw new InvalidOperationException($"Unexpected Execute signature on {dialogEnvQueryInterfaceType.FullName}.");
        }

        var typeBuilder = ModuleBuilder.DefineType(
            $"LongLive.Next.Runtime.Generated.NextDialogEnvQueryProxy{_proxyCounter++}",
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        typeBuilder.AddInterfaceImplementation(dialogEnvQueryInterfaceType);

        var handlerField = typeBuilder.DefineField("_handler", typeof(INextDialogEnvQueryProxyHandler), FieldAttributes.Private | FieldAttributes.InitOnly);
        BuildConstructor(typeBuilder, handlerField);
        BuildExecuteMethod(typeBuilder, dialogEnvQueryInterfaceType, executeMethod, handlerField, parameters[0].ParameterType);

        return typeBuilder.CreateTypeInfo()!.AsType();
    }

    private static void BuildConstructor(TypeBuilder typeBuilder, FieldBuilder handlerField)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { typeof(INextDialogEnvQueryProxyHandler) });

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
        Type dialogEnvQueryInterfaceType,
        MethodInfo interfaceMethod,
        FieldBuilder handlerField,
        Type parameterType)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            interfaceMethod.ReturnType,
            new[] { parameterType });

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, handlerField);
        il.Emit(OpCodes.Ldarg_1);
        if (parameterType.IsValueType)
        {
            il.Emit(OpCodes.Box, parameterType);
        }

        il.Emit(OpCodes.Callvirt, typeof(INextDialogEnvQueryProxyHandler).GetMethod(nameof(INextDialogEnvQueryProxyHandler.Execute))!);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, dialogEnvQueryInterfaceType.GetMethod(interfaceMethod.Name, new[] { parameterType })!);
    }
}
