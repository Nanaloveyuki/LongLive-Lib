param()

$ErrorActionPreference = 'Stop'

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-poptip-runtime-access-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $projectPath = Join-Path $tempDir 'PopTipRuntimeAccessTest.csproj'
    $programPath = Join-Path $tempDir 'Program.cs'

    $program = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
    public static class AccessTools
    {
        public static Type? TypeByName(string name)
        {
            return name == "UIPopTip" ? typeof(UIPopTip) : null;
        }

        public static FieldInfo? Field(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static PropertyInfo? Property(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static MethodInfo? Method(Type? type, string name, Type[] args)
        {
            return type?.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }
    }
}

public sealed class FakeQueue : ArrayList
{
    public new void Clear()
    {
        base.Clear();
    }
}

public sealed class UIPopTip
{
    public static UIPopTip? Inst;

    public float minCD;

    public float tweenDestoryCD;

    public float addItemMergeCD;

    public FakeQueue WaitForShow = new FakeQueue();

    public Dictionary<string, string> addItemMergeMsgDict = new Dictionary<string, string>();

    public ArrayList Tips = new ArrayList();
}

namespace LongLive.BepInEx.Plugin
{
    internal static class LongLivePopTipRuntimeAccess
    {
        private static ConditionalWeakTable<object, TimingSnapshot> TimingSnapshots = new ConditionalWeakTable<object, TimingSnapshot>();

        public static bool TryGetSingleton(out Type popTipType, out object inst)
        {
            popTipType = HarmonyLib.AccessTools.TypeByName("UIPopTip")!;
            inst = null!;
            if (popTipType == null)
            {
                return false;
            }

            var candidate = HarmonyLib.AccessTools.Field(popTipType, "Inst")?.GetValue(null)
                ?? HarmonyLib.AccessTools.Property(popTipType, "Inst")?.GetValue(null, null);
            if (candidate == null)
            {
                return false;
            }

            inst = candidate;
            return true;
        }

        public static void SetTimingFields(Type popTipType, object inst, float minCd, float tweenDestroyCd, float addItemMergeCd)
        {
            HarmonyLib.AccessTools.Field(popTipType, "minCD")?.SetValue(inst, minCd);
            HarmonyLib.AccessTools.Field(popTipType, "tweenDestoryCD")?.SetValue(inst, tweenDestroyCd);
            HarmonyLib.AccessTools.Field(popTipType, "addItemMergeCD")?.SetValue(inst, addItemMergeCd);
        }

        public static float ReadTimingField(Type popTipType, object inst, string fieldName)
        {
            var value = HarmonyLib.AccessTools.Field(popTipType, fieldName)?.GetValue(inst);
            return value is float floatValue ? floatValue : 0f;
        }

        public static void CaptureTimingSnapshotIfNeeded(Type popTipType, object inst)
        {
            if (TimingSnapshots.TryGetValue(inst, out _))
            {
                return;
            }

            TimingSnapshots.Add(inst, new TimingSnapshot(
                ReadTimingField(popTipType, inst, "minCD"),
                ReadTimingField(popTipType, inst, "tweenDestoryCD"),
                ReadTimingField(popTipType, inst, "addItemMergeCD")));
        }

        public static bool TryRestoreTimingSnapshot(Type popTipType, object inst)
        {
            if (!TimingSnapshots.TryGetValue(inst, out var snapshot))
            {
                return false;
            }

            SetTimingFields(popTipType, inst, snapshot.MinCd, snapshot.TweenDestroyCd, snapshot.AddItemMergeCd);
            return true;
        }

        public static void ClearTimingSnapshot(object inst)
        {
            if (inst == null)
            {
                return;
            }

            TimingSnapshots.Remove(inst);
        }

        public static void ClearAllTimingSnapshots()
        {
            TimingSnapshots = new ConditionalWeakTable<object, TimingSnapshot>();
        }

        private sealed class TimingSnapshot
        {
            public TimingSnapshot(float minCd, float tweenDestroyCd, float addItemMergeCd)
            {
                MinCd = minCd;
                TweenDestroyCd = tweenDestroyCd;
                AddItemMergeCd = addItemMergeCd;
            }

            public float MinCd { get; }

            public float TweenDestroyCd { get; }

            public float AddItemMergeCd { get; }
        }
    }
}

internal static class PopTipRuntimeAccessTestProgram
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    public static void Main()
    {
        var first = new UIPopTip { minCD = 0.4f, tweenDestoryCD = 1.5f, addItemMergeCD = 0.8f };
        var second = new UIPopTip { minCD = 0.2f, tweenDestoryCD = 0.9f, addItemMergeCD = 0.3f };

        var type = typeof(UIPopTip);
        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.CaptureTimingSnapshotIfNeeded(type, first);
        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.CaptureTimingSnapshotIfNeeded(type, second);

        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.SetTimingFields(type, first, 0f, 0f, 0f);
        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.SetTimingFields(type, second, 0f, 0f, 0f);

        Assert(LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(type, first), "Expected first instance restore to succeed.");
        Assert(first.minCD == 0.4f && first.tweenDestoryCD == 1.5f && first.addItemMergeCD == 0.8f, "First instance restored wrong values.");

        Assert(LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(type, second), "Expected second instance restore to succeed.");
        Assert(second.minCD == 0.2f && second.tweenDestoryCD == 0.9f && second.addItemMergeCD == 0.3f, "Second instance restored wrong values.");

        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.ClearTimingSnapshot(first);
        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.SetTimingFields(type, first, 9f, 9f, 9f);
        Assert(!LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(type, first), "Expected cleared first snapshot to stop restoring.");
        Assert(first.minCD == 9f && first.tweenDestoryCD == 9f && first.addItemMergeCD == 9f, "First instance should remain unchanged after cleared restore.");

        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.ClearAllTimingSnapshots();
        LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.SetTimingFields(type, second, 7f, 7f, 7f);
        Assert(!LongLive.BepInEx.Plugin.LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(type, second), "Expected global snapshot clear to remove second restore data.");
        Assert(second.minCD == 7f && second.tweenDestoryCD == 7f && second.addItemMergeCD == 7f, "Second instance should remain unchanged after global clear.");

        Console.WriteLine("LongLivePopTipRuntimeAccess tests passed.");
    }
}
'@

    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
"@

    [System.IO.File]::WriteAllText($projectPath, $project, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    dotnet run --project $projectPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Pop-tip runtime access test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
