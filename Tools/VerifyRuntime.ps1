param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $projectRoot 'Temp/LumiaVerification'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$argsList = @((Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'), '/nologo', '/target:library', '/langversion:latest', ('/out:' + (Join-Path $outputDir 'Lumia.Runtime.dll')), '/nostdlib+')
foreach ($name in @('mscorlib.dll','System.dll','System.Core.dll','Facades/netstandard.dll')) { $argsList += '/r:' + (Join-Path $monoLibrary $name) }
Get-ChildItem -LiteralPath (Join-Path $UnityEditor 'Data/Managed/UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object { $argsList += '/r:' + $_.FullName }
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Lumia') -Filter '*.cs' | ForEach-Object { $argsList += $_.FullName }
& (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output 'Runtime C# compilation passed.'
