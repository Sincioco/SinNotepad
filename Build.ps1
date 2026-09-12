param([switch]$Package)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build SinNotepad.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if ($Package) {
        dotnet publish src/SinNotepad/SinNotepad.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=false -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false -o app --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }
        $shortcutShell = New-Object -ComObject WScript.Shell
        $shortcut = $shortcutShell.CreateShortcut((Join-Path $PSScriptRoot 'SinNotePad.lnk'))
        $shortcut.TargetPath = Join-Path $PSScriptRoot 'app\Sin - Notepad.exe'
        $shortcut.WorkingDirectory = Join-Path $PSScriptRoot 'app'
        $shortcut.Description = 'SinNotePad plain-text editor'
        $shortcut.Save()
        Write-Output 'Ready: app\Sin - Notepad.exe (or double-click SinNotePad.lnk)'
    }
} finally { Pop-Location }
