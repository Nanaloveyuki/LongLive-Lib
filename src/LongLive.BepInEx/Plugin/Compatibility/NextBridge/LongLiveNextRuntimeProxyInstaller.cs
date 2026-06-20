using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using LongLive.Next.Runtime.Internal;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveNextRuntimeProxyInstaller
{
    private static readonly ModuleBuilder ModuleBuilder;

    private static readonly ConcurrentDictionary<Type, Type> DialogEventProxyTypes = new ConcurrentDictionary<Type, Type>();

    private static readonly ConcurrentDictionary<Type, Type> DialogEnvQueryProxyTypes = new ConcurrentDictionary<Type, Type>();

    static LongLiveNextRuntimeProxyInstaller()
    {
        var assemblyName = new AssemblyName("LongLive.Next.Runtime.HostProxyAssembly");
        var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule("LongLive.Next.Runtime.HostProxyModule");
    }

    public static void Install()
    {
        NextDialogEventProxyFactory.Factory = CreateDialogEventProxy;
        NextDialogEnvQueryProxyFactory.Factory = CreateDialogEnvQueryProxy;
    }

    private static object CreateDialogEventProxy(Type interfaceType, INextDialogEventProxyHandler handler)
    {
        if (interfaceType is null)
        {
            throw new ArgumentNullException(nameof(interfaceType));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var proxyType = DialogEventProxyTypes.GetOrAdd(interfaceType, BuildDialogEventProxyType);
        return Activator.CreateInstance(proxyType, handler) ?? throw new InvalidOperationException($"Failed to create dialog-event proxy instance for {interfaceType.FullName}.");
    }

    private static object CreateDialogEnvQueryProxy(Type interfaceType, INextDialogEnvQueryProxyHandler handler)
    {
        if (interfaceType is null)
        {
            throw new ArgumentNullException(nameof(interfaceType));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var proxyType = DialogEnvQueryProxyTypes.GetOrAdd(interfaceType, BuildDialogEnvQueryProxyType);
        return Activator.CreateInstance(proxyType, handler) ?? throw new InvalidOperationException($"Failed to create dialog-env-query proxy instance for {interfaceType.FullName}.");
    }

    private static Type BuildDialogEventProxyType(Type interfaceType)
    {
        var typeBuilder = CreateTypeBuilder(interfaceType, "DialogEvent");
        typeBuilder.AddInterfaceImplementation(interfaceType);

        var handlerField = typeBuilder.DefineField("_handler", typeof(INextDialogEventProxyHandler), FieldAttributes.Private | FieldAttributes.InitOnly);
        ImplementConstructor(typeBuilder, handlerField, typeof(INextDialogEventProxyHandler));

        var executeMethod = interfaceType.GetMethod("Execute") ?? throw new MissingMethodException(interfaceType.FullName, "Execute");
        var parameters = executeMethod.GetParameters();
        if (parameters.Length != 3)
        {
            throw new InvalidOperationException($"Unexpected {interfaceType.FullName}.Execute signature. Expected 3 parameters, got {parameters.Length}.");
        }

        var methodBuilder = typeBuilder.DefineMethod(
            executeMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            executeMethod.ReturnType,
            Array.ConvertAll(parameters, static parameter => parameter.ParameterType));

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, handlerField);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Callvirt, typeof(INextDialogEventProxyHandler).GetMethod(nameof(INextDialogEventProxyHandler.Execute))!);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, executeMethod);
        return typeBuilder.CreateType();
    }

    private static Type BuildDialogEnvQueryProxyType(Type interfaceType)
    {
        var typeBuilder = CreateTypeBuilder(interfaceType, "DialogEnvQuery");
        typeBuilder.AddInterfaceImplementation(interfaceType);

        var handlerField = typeBuilder.DefineField("_handler", typeof(INextDialogEnvQueryProxyHandler), FieldAttributes.Private | FieldAttributes.InitOnly);
        ImplementConstructor(typeBuilder, handlerField, typeof(INextDialogEnvQueryProxyHandler));

        var executeMethod = interfaceType.GetMethod("Execute") ?? throw new MissingMethodException(interfaceType.FullName, "Execute");
        var parameters = executeMethod.GetParameters();
        if (parameters.Length != 1)
        {
            throw new InvalidOperationException($"Unexpected {interfaceType.FullName}.Execute signature. Expected 1 parameter, got {parameters.Length}.");
        }

        var methodBuilder = typeBuilder.DefineMethod(
            executeMethod.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            executeMethod.ReturnType,
            Array.ConvertAll(parameters, static parameter => parameter.ParameterType));

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, handlerField);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, typeof(INextDialogEnvQueryProxyHandler).GetMethod(nameof(INextDialogEnvQueryProxyHandler.Execute))!);

        if (executeMethod.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            EmitReturnConversion(il, executeMethod.ReturnType);
            il.Emit(OpCodes.Ret);
        }

        typeBuilder.DefineMethodOverride(methodBuilder, executeMethod);
        return typeBuilder.CreateType();
    }

    private static TypeBuilder CreateTypeBuilder(Type interfaceType, string prefix)
    {
        var typeName = $"LongLive.Dynamic.{prefix}.{SanitizeTypeName(interfaceType.FullName ?? interfaceType.Name)}_{Guid.NewGuid():N}";
        return ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
    }

    private static void ImplementConstructor(TypeBuilder typeBuilder, FieldBuilder handlerField, Type handlerType)
    {
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { handlerType });
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, handlerField);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitReturnConversion(ILGenerator il, Type returnType)
    {
        if (returnType == typeof(object))
        {
            return;
        }

        if (returnType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, returnType);
            return;
        }

        il.Emit(OpCodes.Castclass, returnType);
    }

    private static string SanitizeTypeName(string typeName)
    {
        return typeName
            .Replace('.', '_')
            .Replace('+', '_')
            .Replace('`', '_');
    }
}
