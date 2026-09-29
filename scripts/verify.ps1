<#
.SYNOPSIS
  Builds and tests AutoTyper, printing only what matters: compiler errors,
  failed tests (name, message, first in-repo stack frame) and totals.

.DESCRIPTION
  Runs `dotnet test` on each test project, which builds exactly what that
  project needs — no separate `dotnet build`, and AutoTyper.Harness (the
  Windows-only WPF dev tool) is never built. Exits non-zero on any failure.

.PARAMETER Filter
  Passed through to `dotnet test --filter`, for fast inner-loop runs,
  e.g. -Filter "FullyQualifiedName~TypingEngineTests".

.PARAMETER Project
  Limit the run to one test project: Core or Desktop. Default runs both.
#>
param(
    [string]$Filter,
    [ValidateSet('All', 'Core', 'Desktop')]
    [string]$Project = 'All'
)

$root = Split-Path -Parent $PSScriptRoot
$projects = @()
if ($Project -in 'All', 'Core')    { $projects += 'AutoTyper.Core.Tests' }
if ($Project -in 'All', 'Desktop') { $projects += 'AutoTyper.Desktop.Tests' }

$failed = $false
$sw = [Diagnostics.Stopwatch]::StartNew()

foreach ($name in $projects) {
    # -v q keeps MSBuild quiet; the console logger still needs normal
    # verbosity or it omits failure messages and stack traces.
    $testArgs = @('test', (Join-Path $root "$name\$name.csproj"), '--nologo', '-v', 'q',
                  '--logger', 'console;verbosity=normal')
    if ($Filter) { $testArgs += @('--filter', $Filter) }

    $lines = & dotnet @testArgs 2>&1 | ForEach-Object { "$_" }
    $exit = $LASTEXITCODE

    # MSBuild repeats each compiler error in its summary; show each once.
    $errors = $lines | Where-Object { $_ -match ': error [A-Z]+\d+' } | Select-Object -Unique
    if ($errors) {
        Write-Output "[$name] BUILD FAILED"
        # Drop the repo-root prefix and MSBuild's trailing [project.csproj].
        $errors | ForEach-Object {
            Write-Output ("  " + ($_.Trim().Replace("$root\", '') -replace '\s*\[[^\]]+\.csproj\]$', ''))
        }
        $failed = $true
        continue
    }

    # For each failed test: its name, the error message, and the first stack
    # frame pointing into the repo (skips framework frames).
    $inFailure = $false; $inStack = $false; $shownFrame = $false
    foreach ($line in $lines) {
        if ($line -match '^\s*Failed (\S+) \[') {
            Write-Output "[$name] FAILED $($Matches[1])"
            $inFailure = $true; $inStack = $false; $shownFrame = $false
            continue
        }
        if (-not $inFailure) { continue }
        if ($line -match '^\s*(Passed|Skipped|Failed)[ !]') { $inFailure = $false; continue }
        if ($line -match '^\s*Stack Trace:') { $inStack = $true; continue }
        if ($line -match '^\s*Error Message:') { continue }
        if (-not $inStack) {
            if ($line.Trim()) { Write-Output "    $($line.Trim())" }
        } elseif (-not $shownFrame -and $line -match ' in (.+):line (\d+)') {
            $file = $Matches[1]; $ln = $Matches[2]
            if ($file.StartsWith($root)) { $file = $file.Substring($root.Length + 1) }
            Write-Output "    at ${file}:$ln"
            $shownFrame = $true
        }
    }

    # Normal-verbosity summary: "Total tests: N" then "Passed: N" / "Failed: N" / "Skipped: N".
    $count = @{}
    foreach ($line in $lines) {
        if ($line -match '^\s*(Total tests|Passed|Failed|Skipped):\s*(\d+)\s*$') { $count[$Matches[1]] = [int]$Matches[2] }
    }
    if ($count.ContainsKey('Total tests')) {
        Write-Output ("[$name] {0} passed, {1} failed, {2} skipped" -f
            [int]$count['Passed'], [int]$count['Failed'], [int]$count['Skipped'])
    } elseif ($exit -ne 0) {
        # Non-zero exit with nothing recognizable: show the tail so the cause isn't hidden.
        Write-Output "[$name] dotnet test exited $exit; last output:"
        $lines | Select-Object -Last 15 | ForEach-Object { Write-Output "  $_" }
    } else {
        Write-Output "[$name] no tests matched"
    }
    if ($exit -ne 0 -and $count['Failed'] -eq 0 -and $count.ContainsKey('Total tests')) {
        Write-Output "[$name] dotnet test exited $exit with no failed tests (test host crash or aborted run?)"
    }
    if ($exit -ne 0) { $failed = $true }
}

$verdict = if ($failed) { 'FAIL' } else { 'PASS' }
Write-Output ("VERDICT: {0} ({1:n1}s)" -f $verdict, $sw.Elapsed.TotalSeconds)
if ($failed) { exit 1 } else { exit 0 }
