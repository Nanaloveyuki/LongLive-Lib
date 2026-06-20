param()

$ErrorActionPreference = 'Stop'

$program = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public sealed class CodeInstruction
{
    public CodeInstruction(OpCode opcode)
    {
        this.opcode = opcode;
    }

    public CodeInstruction(OpCode opcode, object operand)
    {
        this.opcode = opcode;
        this.operand = operand;
    }

    public OpCode opcode;

    public object operand;
}

namespace LongLive.BepInEx.Plugin
{
    internal static class LongLiveFadeOptimizationRuntime
    {
        public static readonly Dictionary<string, int> Hits = new Dictionary<string, int>(StringComparer.Ordinal);

        public static void ReportTranspilerPatchResult(string patchKey, float targetValue, int hitCount)
        {
            Hits[patchKey] = hitCount;
        }

        public static float GetMapDoorTransitionDelay(float originalSeconds)
        {
            return originalSeconds;
        }
    }

    internal static class LongLiveFadeTranspilerTools
    {
        public static IEnumerable<CodeInstruction> WrapFloatConstantWithCall(IEnumerable<CodeInstruction> instructions, float targetValue, string patchKey, MethodInfo method)
        {
            var hitCount = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float && Math.Abs((float)instruction.operand - targetValue) < 0.0001f)
                {
                    hitCount++;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, method);
                    continue;
                }

                yield return instruction;
            }

            LongLiveFadeOptimizationRuntime.ReportTranspilerPatchResult(patchKey, targetValue, hitCount);
        }
    }
}

internal static class FadeTranspilerTestProgram
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
        var method = typeof(LongLive.BepInEx.Plugin.LongLiveFadeOptimizationRuntime)
            .GetMethod("GetMapDoorTransitionDelay", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            throw new Exception("Target method not found.");
        }

        var original = new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldc_R4, 0.75f),
            new CodeInstruction(OpCodes.Ldc_R4, 0.75f),
            new CodeInstruction(OpCodes.Ldc_R4, 1.1f),
            new CodeInstruction(OpCodes.Ret),
        };

        var patched = LongLive.BepInEx.Plugin.LongLiveFadeTranspilerTools
            .WrapFloatConstantWithCall(original, 0.75f, "KameraMovement.closeDoorAndPlay.delay", method)
            .ToArray();

        var callCount = patched.Count(instruction => instruction.opcode == OpCodes.Call);
        Assert(callCount == 2, "Expected exactly two injected calls for the duplicated 0.75f constants.");
        int hits;
        Assert(LongLive.BepInEx.Plugin.LongLiveFadeOptimizationRuntime.Hits.TryGetValue("KameraMovement.closeDoorAndPlay.delay", out hits) && hits == 2,
            "Expected hit count 2 for the target patch key.");

        var noHitPatched = LongLive.BepInEx.Plugin.LongLiveFadeTranspilerTools
            .WrapFloatConstantWithCall(original, 9.9f, "NoHit", method)
            .ToArray();

        var noHitCallCount = noHitPatched.Count(instruction => instruction.opcode == OpCodes.Call);
        Assert(noHitCallCount == 0, "Expected zero injected calls when the constant is absent.");
        int noHit;
        Assert(LongLive.BepInEx.Plugin.LongLiveFadeOptimizationRuntime.Hits.TryGetValue("NoHit", out noHit) && noHit == 0,
            "Expected zero hit count for the no-hit patch key.");

        VerifySingleHit(method, 1f, "Loading.unistiObjekat.delay");
        VerifySingleHit(method, 1.1f, "AllMapsManageFull.UcitajOstrvo.delay");
        VerifySingleHit(method, 1.1f, "KameraMovement.UcitajOstrvo.delay");
        VerifySingleHit(method, 0.75f, "Manage.closeDoorAndPlay.delay");
        VerifySingleHit(method, 1.1f, "MainScene.otvoriSledeciNivo.delay");

        var scaleOnlyMethod = typeof(LongLive.BepInEx.Plugin.LongLiveFadeOptimizationRuntime)
            .GetMethod("GetMapDoorTransitionDelay", BindingFlags.Public | BindingFlags.Static);
        if (scaleOnlyMethod == null)
        {
            throw new Exception("Scale-only target method not found.");
        }

        VerifySingleHit(scaleOnlyMethod, 0.25f, "MainMenuManage.otvoriSledeciNivo.delay");

        Console.WriteLine("LongLiveFadeTranspiler tests passed.");
    }

    private static void VerifySingleHit(MethodInfo method, float constant, string patchKey)
    {
        var sample = new[]
        {
            new CodeInstruction(OpCodes.Ldc_R4, constant),
            new CodeInstruction(OpCodes.Ret),
        };

        var patched = LongLive.BepInEx.Plugin.LongLiveFadeTranspilerTools
            .WrapFloatConstantWithCall(sample, constant, patchKey, method)
            .ToArray();

        var callCount = patched.Count(instruction => instruction.opcode == OpCodes.Call);
        Assert(callCount == 1, "Expected one injected call for patch key " + patchKey + ".");

        int hits;
        Assert(LongLive.BepInEx.Plugin.LongLiveFadeOptimizationRuntime.Hits.TryGetValue(patchKey, out hits) && hits == 1,
            "Expected hit count 1 for patch key " + patchKey + ".");
    }
}
'@

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-fade-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $programPath = Join-Path $tempDir 'FadeTranspilerTestProgram.cs'
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $compiler)) {
        $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }

    if (-not (Test-Path $compiler)) {
        throw 'CSC compiler not found in .NET Framework directories.'
    }

    $outputPath = Join-Path $tempDir 'FadeTranspilerTestProgram.exe'
    & $compiler /nologo /t:exe /out:$outputPath $programPath
    if ($LASTEXITCODE -ne 0) {
        throw "Fade transpiler test compilation failed with exit code $LASTEXITCODE"
    }

    & $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Fade transpiler test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
