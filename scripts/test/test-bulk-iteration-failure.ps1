param()

$ErrorActionPreference = 'Stop'

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-bulk-iteration-failure-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $projectPath = Join-Path $tempDir 'BulkIterationFailureTest.csproj'
    $programPath = Join-Path $tempDir 'Program.cs'

    $program = @'
using System;

namespace LongLive.BepInEx.Plugin
{
    internal sealed class BulkUseRequest
    {
        public BulkUseRequest(int remaining)
        {
            Remaining = remaining;
        }

        public int Remaining { get; set; }

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }
    }

    internal static class BulkIterationFailureHarness
    {
        public static int CompletedCount { get; private set; }

        public static void Reset()
        {
            CompletedCount = 0;
        }

        public static void RunOneIteration(BulkUseRequest request, Action useAction)
        {
            try
            {
                useAction();
                request.Remaining--;
                CompletedCount++;
            }
            catch
            {
                request.Cancel();
            }
        }
    }
}

internal static class BulkIterationFailureTestProgram
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
        var request = new LongLive.BepInEx.Plugin.BulkUseRequest(3);
        LongLive.BepInEx.Plugin.BulkIterationFailureHarness.Reset();

        LongLive.BepInEx.Plugin.BulkIterationFailureHarness.RunOneIteration(request, () => { });
        Assert(request.Remaining == 2, "Expected first successful iteration to decrement remaining.");
        Assert(LongLive.BepInEx.Plugin.BulkIterationFailureHarness.CompletedCount == 1, "Expected first successful iteration to increment completed count.");
        Assert(!request.Cancelled, "Request should still be active after success.");

        LongLive.BepInEx.Plugin.BulkIterationFailureHarness.RunOneIteration(request, () => throw new InvalidOperationException("boom"));
        Assert(request.Remaining == 2, "Failed iteration should not decrement remaining.");
        Assert(LongLive.BepInEx.Plugin.BulkIterationFailureHarness.CompletedCount == 1, "Failed iteration should not increment completed count.");
        Assert(request.Cancelled, "Failed iteration should cancel the request.");

        Console.WriteLine("LongLiveBulkIterationFailure tests passed.");
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
        throw "Bulk iteration failure test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
