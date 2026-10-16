param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'Temp/BrowserAccountStorageTests.exe'
[IO.Directory]::CreateDirectory((Split-Path -Parent $outputPath)) | Out-Null
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$arguments = @((Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'), '/nologo', '/target:exe', '/langversion:latest', ('/out:' + $outputPath), '/nostdlib+', ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), (Join-Path $projectRoot 'Assets/Scripts/Lumia/BrowserAccountStorage.cs'), (Join-Path $PSScriptRoot 'BrowserAccountStorageSmokeTests.cs'))
& (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
try { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath; $testExit = $LASTEXITCODE }
finally { Remove-Item -LiteralPath $outputPath -Force -ErrorAction SilentlyContinue }
exit $testExit
