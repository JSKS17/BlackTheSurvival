param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor',
    [string]$Output = 'Temp/AudioAudit/catalog-audit.json'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot 'Temp/AudioAudit'
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$executablePath = Join-Path $outputDirectory 'AudioAudit.exe'
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$compilerArguments = @((Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'), '/nologo', '/target:exe', '/langversion:latest', ('/out:' + $executablePath), '/nostdlib+', (Join-Path $PSScriptRoot 'AudioAudit.cs'))
foreach ($name in @('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Web.Extensions.dll')) { $compilerArguments += '/r:' + (Join-Path $monoLibrary $name) }
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Lumia') -Filter '*.cs' | ForEach-Object {
    if ((Get-Content -LiteralPath $_.FullName -Raw) -notmatch 'using UnityEngine') { $compilerArguments += $_.FullName }
}
& (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') @compilerArguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$reportPath = if ([IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $projectRoot $Output }
& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executablePath $projectRoot $reportPath
exit $LASTEXITCODE
