$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & (Join-Path $PSScriptRoot 'Check-Architecture.ps1') -SelfTest
    dotnet build SinNotepad.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet run --project tests/SinNotepad.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    $testData = Join-Path $PSScriptRoot ('work\ui-tests-' + [Guid]::NewGuid().ToString('N'))
    $testExe = Join-Path $PSScriptRoot 'src\SinNotepad\bin\Release\net10.0-windows\Sin - Notepad.exe'
    $testArguments = '--self-test --data-dir "' + $testData + '"'
    $testProcess = Start-Process -FilePath $testExe -ArgumentList $testArguments -WindowStyle Hidden -PassThru
    if (!$testProcess.WaitForExit(30000)) { throw "UI tests have not finished. Process $($testProcess.Id); data: $testData" }
    Get-Content -LiteralPath (Join-Path $testData 'ui-test-results.txt')
    if ($testProcess.ExitCode -ne 0) { throw 'UI tests failed.' }
    Write-Output 'All tests passed.'
} finally { Pop-Location }
