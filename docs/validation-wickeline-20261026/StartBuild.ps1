#requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('Web','Windows')][string]$Target='Web')
$ErrorActionPreference='Stop'
$wickRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location -LiteralPath $wickRoot
$wickClone=Join-Path $wickRoot 'Build\UnityWebBuild'
if(@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($wickClone,[StringComparison]::OrdinalIgnoreCase)}).Count) {throw 'The owned Unity clone is already open.'}
$wickFreeze=Join-Path $PSScriptRoot 'source-validation.json'
$wickSources=Get-Content -LiteralPath $wickFreeze -Raw | ConvertFrom-Json -AsHashtable
foreach($wickMap in @($wickSources.files,$wickSources.testSourceHashes,$wickSources.artSourceHashes)) {
    foreach($wickEntry in $wickMap.GetEnumerator()) {
        $wickFile=[IO.Path]::GetFullPath((Join-Path $wickRoot $wickEntry.Key))
        if(-not $wickFile.StartsWith($wickRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Source path escaped workspace.'}
        if((Get-FileHash -LiteralPath $wickFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $wickEntry.Value) {throw ('Frozen source changed: '+$wickEntry.Key)}
    }
}
if($Target -eq 'Web') {
    & (Join-Path $wickRoot 'Tools\BuildWeb.ps1') -PrepareOnly -LowMemory -OutputDirectory 'Build/Web127' | Out-Null
    $wickLog=Join-Path $wickRoot 'Build\LumiaWebBuild.log'
    $wickArgs=@('-batchmode','-nographics','-buildTarget','WebGL','-projectPath',$wickClone,'-executeMethod','Lumia.Editor.WickelineReleaseBuild.Build','-logFile',$wickLog,'-lumia-web-output',(Join-Path $wickRoot 'Build\Web127'),'-lumia-web-compression','Gzip','-lumia-fixture-output',(Join-Path $PSScriptRoot 'fixtures'))
} else {
    if(-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'build-source-validation.json'))) {throw 'Web source snapshot must pass before reusing clone for Windows.'}
    foreach($wickMap in @($wickSources.files,$wickSources.artSourceHashes)) {
        foreach($wickEntry in $wickMap.GetEnumerator()) {
            if(-not $wickEntry.Key.StartsWith('Assets/')) {continue}
            if((Get-FileHash -LiteralPath (Join-Path $wickClone $wickEntry.Key) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $wickEntry.Value) {throw ('Cloned source changed: '+$wickEntry.Key)}
        }
    }
    $wickLog=Join-Path $PSScriptRoot 'unity-windows.log'
    $wickArgs=@('-batchmode','-nographics','-buildTarget','StandaloneWindows64','-projectPath',$wickClone,'-executeMethod','Lumia.Editor.LumiaProjectSetup.BuildWindows','-logFile',$wickLog,'-lumia-source-capture',$wickFreeze,'--burst-disable-compilation')
}
$wickArgs+=@('-job-worker-count','1','-import-worker-count','1')
$wickPrevious=[Environment]::GetEnvironmentVariable('BEE_BUILD_THREADS','Process')
try {
    [Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS','1','Process')
    $wickProcess=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' -ArgumentList $wickArgs -WindowStyle Hidden -PassThru
} finally { [Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS',$wickPrevious,'Process') }
$wickRecord=[ordered]@{target=$Target;processId=$wickProcess.Id;startedAtUtc=$wickProcess.StartTime.ToUniversalTime().ToString('o');log=$wickLog;project=$wickClone;sourceCaptureSha256=(Get-FileHash -LiteralPath $wickFreeze -Algorithm SHA256).Hash.ToLowerInvariant()}
$wickRecord | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('build-process-'+$Target.ToLowerInvariant()+'.json')) -Encoding UTF8
$wickRecord | ConvertTo-Json
