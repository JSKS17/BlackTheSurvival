param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot 'Temp/DescriptionAudit'
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$executablePath = Join-Path $outputDirectory 'DescriptionAudit.exe'
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$runtimePath = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compilerPath = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$arguments = @($compilerPath, '/nologo', '/target:exe', '/langversion:latest', ('/out:' + $executablePath), '/nostdlib+', ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), (Join-Path $PSScriptRoot 'DescriptionAudit.cs'))
# Include the pure rules/data files so new weapon or equipment definitions are audited too.
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Lumia') -Filter '*.cs' | ForEach-Object {
    if ((Get-Content -LiteralPath $_.FullName -Raw) -notmatch 'using UnityEngine') { $arguments += $_.FullName }
}
& $runtimePath @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$catalogPath = Join-Path $projectRoot 'docs/DESCRIPTION_SUMMARIES.md'
& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executablePath $catalogPath
exit $LASTEXITCODE
