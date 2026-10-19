param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$coreAssembly = Join-Path $workspace 'Temp/CoreTests.exe'
if (-not (Test-Path -LiteralPath $coreAssembly)) { throw 'Run Tools/RunCoreTests.ps1 before exporting live tactical definitions.' }
$destination = Join-Path $workspace 'Temp/TacticalPreview'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$executable = Join-Path $workspace 'Temp/ExportTacticalPreview.exe'
$arguments = @((Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'), '/nologo', '/target:exe', '/langversion:latest',
    ('/out:' + $executable), '/nostdlib+', ('/r:' + $coreAssembly),
    ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')),
    ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), (Join-Path $PSScriptRoot 'ExportTacticalPreview.cs'))
& (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executable (Join-Path $destination 'runtime-cards.json')
exit $LASTEXITCODE
