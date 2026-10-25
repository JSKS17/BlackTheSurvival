param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor', [string]$TouchSource='', [string]$EvidenceName='combat-card-touch-tests.json')
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$temporaryParent=Join-Path $workspace 'Temp/CombatCardTouchTests'
$temporary=[IO.Path]::GetFullPath((Join-Path $temporaryParent ([guid]::NewGuid().ToString('N'))))
$scope=[IO.Path]::GetFullPath($temporaryParent)+[IO.Path]::DirectorySeparatorChar
if(-not $temporary.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid temporary test scope'}
[IO.Directory]::CreateDirectory($temporary)|Out-Null
$executable=Join-Path $temporary 'CombatCardTouchTests.exe'
if(-not $TouchSource){$TouchSource=Join-Path $workspace 'Assets/Scripts/Lumia/LumiaTouchScroll.cs'}
if($EvidenceName -notmatch '^combat-card-touch-[a-z-]*tests\.json$'){throw 'Invalid evidence filename'}
$sources=@((Join-Path $workspace 'Assets/Scripts/Lumia/TouchScrollGesture.cs'),$TouchSource,(Join-Path $PSScriptRoot 'CombatCardTouchIntegrationTests.cs'))
$evidence=Join-Path $workspace ('docs/validation-combat-card-scroll-20261022/'+$EvidenceName)
$clock=[Diagnostics.Stopwatch]::StartNew();$exitCode=1;$report=$null
try {
    $monoLibrary=Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
    $arguments=@((Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'),'/nologo','/target:exe','/langversion:latest','/define:ENABLE_LEGACY_INPUT_MANAGER','/warn:0',('/out:'+$executable),'/nostdlib+',('/r:'+(Join-Path $monoLibrary 'mscorlib.dll')),('/r:'+(Join-Path $monoLibrary 'System.dll')),('/r:'+(Join-Path $monoLibrary 'System.Core.dll')))+$sources
    & (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') @arguments
    if($LASTEXITCODE -ne 0){throw 'Integration tests did not compile'}
    $output=@(& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executable 2>&1|ForEach-Object{$_.ToString()})
    $output|ForEach-Object{Write-Output $_}
    $marker=$output|Where-Object{$_.StartsWith('COMBAT_CARD_TOUCH_RESULT ')}|Select-Object -Last 1
    if(-not $marker){throw 'No integration result marker'}
    $result=$marker.Substring('COMBAT_CARD_TOUCH_RESULT '.Length)|ConvertFrom-Json
    $hashes=[ordered]@{};foreach($file in $sources+@($PSCommandPath)){$hashes[$file.Substring($workspace.Length+1).Replace('\','/')]=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
    $report=[ordered]@{result=$result.result;checkedAtUtc=[datetime]::UtcNow.ToString('o');assertions=$result.assertions;scenarios=$result.scenarios;failedScenarios=@($result.failedScenarios|Where-Object{$_});failureDetails=@($output|Where-Object{$_.StartsWith('CASE_FAIL ')});sourceHashes=$hashes;scope='Production LumiaTouchScroll + TouchScrollGesture compiled directly; deterministic Unity host doubles; no physical browser touch claim'}
    if($result.result -eq 'PASS'){$exitCode=0}
} catch {$report=[ordered]@{result='FAIL';checkedAtUtc=[datetime]::UtcNow.ToString('o');error=$_.Exception.Message};Write-Error -Message $_.Exception.Message -ErrorAction Continue}
finally {
    $resolved=[IO.Path]::GetFullPath($temporary)
    if(-not $resolved.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing cleanup outside test scope'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
    if((Test-Path -LiteralPath $temporaryParent)-and @(Get-ChildItem -LiteralPath $temporaryParent -Force).Count -eq 0){Remove-Item -LiteralPath $temporaryParent -Force}
    $clock.Stop();$report['durationSeconds']=$clock.Elapsed.TotalSeconds;$report['temporaryFilesRemoved']=$true
    [IO.Directory]::CreateDirectory((Split-Path -Parent $evidence))|Out-Null
    $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $evidence -Encoding utf8
}
exit $exitCode
