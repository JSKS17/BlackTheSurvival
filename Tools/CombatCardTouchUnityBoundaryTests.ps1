param([string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parent=Join-Path $workspace 'Temp/CombatCardTouchBoundary'
$temporary=[IO.Path]::GetFullPath((Join-Path $parent ([guid]::NewGuid().ToString('N'))))
$scope=[IO.Path]::GetFullPath($parent)+[IO.Path]::DirectorySeparatorChar
if(-not $temporary.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid boundary test scope'}
[IO.Directory]::CreateDirectory($temporary)|Out-Null
try {
    $mono=Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
    $unity=Join-Path $UnityEditor 'Data/Managed/UnityEngine'
    $exe=Join-Path $temporary 'Boundary.exe'
    $source=Join-Path $PSScriptRoot 'CombatCardTouchUnityBoundaryTests.cs'
    $module=Join-Path $unity 'UnityEngine.IMGUIModule.dll'
    & (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll') /nologo /target:exe /langversion:latest ('/out:'+$exe) /nostdlib+ ('/r:'+(Join-Path $mono 'mscorlib.dll')) ('/r:'+(Join-Path $mono 'System.dll')) ('/r:'+(Join-Path $mono 'System.Core.dll')) ('/r:'+(Join-Path $mono 'Facades/netstandard.dll')) ('/r:'+(Join-Path $unity 'UnityEngine.CoreModule.dll')) ('/r:'+$module) $source
    if($LASTEXITCODE -ne 0){throw 'Unity boundary inspection did not compile'}
    $originalMonoPath=$env:MONO_PATH
    try {$env:MONO_PATH=$unity;$output=@(& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $exe 2>&1|ForEach-Object{$_.ToString()})}
    finally {$env:MONO_PATH=$originalMonoPath}
    if($LASTEXITCODE -ne 0){throw 'Unity boundary inspection failed'}
    $output|ForEach-Object{Write-Output $_}
    $marker=$output|Where-Object{$_.TrimStart([char]0xfeff).StartsWith('UNITY_GUI_BOUNDARY_RESULT ')}|Select-Object -Last 1
    if(-not $marker){throw 'No Unity boundary result marker'}
    $result=$marker.TrimStart([char]0xfeff).Substring('UNITY_GUI_BOUNDARY_RESULT '.Length)|ConvertFrom-Json
    $evidence=[ordered]@{result=$result.result;checkedAtUtc=[datetime]::UtcNow.ToString('o');unityEditor=$UnityEditor;moduleSha256=(Get-FileHash -LiteralPath $module -Algorithm SHA256).Hash.ToLowerInvariant();sourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();findings=$result;scope='Reflection of actual installed Unity managed IL; native GUI APIs not invoked'}
    $file=Join-Path $workspace 'docs/validation-combat-card-scroll-20261022/unity-boundary-tests.json'
    $evidence|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $file -Encoding utf8
} finally {
    $resolved=[IO.Path]::GetFullPath($temporary)
    if(-not $resolved.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing cleanup outside boundary scope'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
    if((Test-Path -LiteralPath $parent)-and @(Get-ChildItem -LiteralPath $parent -Force).Count -eq 0){Remove-Item -LiteralPath $parent -Force}
}
