param([switch]$SelfTest)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-GuardrailLevel([string]$kind, [int]$lineCount, [int]$legacyBaseline = 0) {
    if ($legacyBaseline -gt 0 -and $lineCount -gt $legacyBaseline) { return 'Failure' }
    if ($kind -eq 'Entry') {
        if ($lineCount -gt 300) { return 'Failure' }
        if ($lineCount -gt 200) { return 'Warning' }
    } else {
        if ($lineCount -gt 800) { return 'Failure' }
        if ($lineCount -gt 500) { return 'Warning' }
    }
    return 'Pass'
}

if ($SelfTest) {
    $cases = @(
        @('Pass', (Get-GuardrailLevel Entry 200)),
        @('Warning', (Get-GuardrailLevel Entry 201)),
        @('Failure', (Get-GuardrailLevel Entry 301)),
        @('Warning', (Get-GuardrailLevel Module 501)),
        @('Failure', (Get-GuardrailLevel Module 585 584))
    )
    foreach ($case in $cases) {
        if ($case[0] -ne $case[1]) { throw "Architecture checker self-test expected $($case[0]) but received $($case[1])." }
    }
    Write-Output 'PASS Architecture checker warning, failure, and legacy-baseline behavior'
}

$repositoryRoot = $PSScriptRoot
$entryFiles = @('src/SinNotepad/App.xaml.cs')
$legacyBaselines = @{
    # Existing window-command coordinator. Any growth requires a reviewed extraction or explicit policy change.
    'src/SinNotepad/MainWindow.xaml.cs' = 584
}
$violations = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

$files = @(
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs', '.xaml' }
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'tests') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    Get-ChildItem -LiteralPath $repositoryRoot -File -Filter '*.ps1'
)

foreach ($file in $files) {
    $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $file.FullName).Replace('\', '/')
    $lineCount = (Get-Content -LiteralPath $file.FullName).Count
    $kind = if ($entryFiles -contains $relativePath) { 'Entry' } else { 'Module' }
    $baseline = if ($legacyBaselines.ContainsKey($relativePath)) { $legacyBaselines[$relativePath] } else { 0 }
    $level = Get-GuardrailLevel $kind $lineCount $baseline
    if ($level -eq 'Failure') {
        $reason = if ($baseline -gt 0 -and $lineCount -gt $baseline) { "legacy baseline $baseline" } elseif ($kind -eq 'Entry') { 'entry hard limit 300' } else { 'module hard limit 800' }
        $violations.Add("$relativePath has $lineCount lines; exceeds $reason.")
    } elseif ($level -eq 'Warning') {
        $limit = if ($kind -eq 'Entry') { 200 } else { 500 }
        $suffix = if ($baseline -gt 0) { "; no-growth baseline $baseline" } else { '' }
        $warnings.Add("$relativePath has $lineCount lines; review warning is $limit$suffix.")
    }
    if ($baseline -gt 0 -and $lineCount -lt $baseline) {
        $warnings.Add("$relativePath is below its ${baseline}-line legacy baseline; review and tighten the recorded baseline to $lineCount.")
    }
}

foreach ($path in $legacyBaselines.Keys) {
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $path))) {
        $violations.Add("Legacy baseline path is missing: $path. Preserve its history when moving or replacing it.")
    }
}

$coreProject = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/SinNotepad.Core/SinNotepad.Core.csproj') -Raw
if ($coreProject -match '<ProjectReference' -or $coreProject -match '<UseWPF>true</UseWPF>') {
    $violations.Add('SinNotepad.Core must not reference the WPF application or enable WPF.')
}
$coreWindowsUsages = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src/SinNotepad.Core') -File -Filter '*.cs' |
    Select-String -Pattern '^using System\.Windows|System\.Windows\.'
if ($coreWindowsUsages) { $violations.Add('SinNotepad.Core source must not depend on WPF namespaces.') }

$appProject = Get-Content -LiteralPath (Join-Path $repositoryRoot 'src/SinNotepad/SinNotepad.csproj') -Raw
if ($appProject -notmatch 'ProjectReference Include="\.\./SinNotepad\.Core/SinNotepad\.Core\.csproj"') {
    $violations.Add('The WPF application must keep its explicit one-way reference to SinNotepad.Core.')
}
$testProject = Get-Content -LiteralPath (Join-Path $repositoryRoot 'tests/SinNotepad.Tests/SinNotepad.Tests.csproj') -Raw
if ($testProject -match 'src/SinNotepad/SinNotepad\.csproj') {
    $violations.Add('Core tests must not acquire a reverse dependency on the WPF application.')
}

foreach ($warning in $warnings) { Write-Warning $warning }
if ($violations.Count -gt 0) {
    foreach ($violation in $violations) { Write-Error $violation }
    throw "Architecture guardrails failed with $($violations.Count) violation(s)."
}

Write-Output "PASS Architecture guardrails: $($files.Count) handwritten source files checked, $($warnings.Count) review warning(s), 0 violations"
