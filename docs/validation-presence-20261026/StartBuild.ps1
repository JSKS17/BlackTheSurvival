#requires -Version 7.0
$ErrorActionPreference='Stop'
$presenceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location -LiteralPath $presenceRoot
$presenceClone=Join-Path $presenceRoot 'Build\UnityWebBuild'
$presenceImport=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'import-process.json') -Raw | ConvertFrom-Json
while(Get-Process -Id $presenceImport.processId -ErrorAction SilentlyContinue) {
    [ordered]@{phase='Importing WebGL assets';imports=(Select-String -LiteralPath (Join-Path $PSScriptRoot 'unity-import.log') -Pattern '^Start importing').Count;atUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json -Compress
    Start-Sleep -Seconds 30
}
if(@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($presenceClone,[StringComparison]::OrdinalIgnoreCase)}).Count) {throw 'Owned Unity clone still open.'}
$presenceFrozen=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-validation.json') -Raw | ConvertFrom-Json -AsHashtable
foreach($presenceMap in @($presenceFrozen.product,$presenceFrozen.unchangedArt)) {foreach($presenceEntry in $presenceMap.GetEnumerator()) {if((Get-FileHash -LiteralPath (Join-Path $presenceRoot $presenceEntry.Key) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $presenceEntry.Value) {throw ('Frozen root changed: '+$presenceEntry.Key)}}}
& (Join-Path $presenceRoot 'Tools\BuildWeb.ps1') -PrepareOnly -LowMemory -OutputDirectory Build/Web128 | Out-Null
foreach($presenceMap in @($presenceFrozen.product,$presenceFrozen.unchangedArt)) {foreach($presenceEntry in $presenceMap.GetEnumerator()) {if($presenceEntry.Key.StartsWith('Assets/')) {if((Get-FileHash -LiteralPath (Join-Path $presenceClone $presenceEntry.Key) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $presenceEntry.Value) {throw ('Cloned root changed: '+$presenceEntry.Key)}}}}
$presenceLog=Join-Path $PSScriptRoot 'unity-build.log'
$presenceArgs=@('-batchmode','-nographics','-buildTarget','WebGL','-projectPath',$presenceClone,'-executeMethod','Lumia.Editor.LumiaWebBuild.Build','-logFile',$presenceLog,'-lumia-web-output',(Join-Path $presenceRoot 'Build\Web128'),'-lumia-web-compression','Gzip','-job-worker-count','1','-import-worker-count','1')
$presencePrevious=[Environment]::GetEnvironmentVariable('BEE_BUILD_THREADS','Process')
try {[Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS','1','Process');$presenceBuild=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' -ArgumentList $presenceArgs -WindowStyle Hidden -PassThru}
finally {[Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS',$presencePrevious,'Process')}
[ordered]@{processId=$presenceBuild.Id;startedAtUtc=$presenceBuild.StartTime.ToUniversalTime().ToString('o');log=$presenceLog;sourceFreezeSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'source-validation.json') -Algorithm SHA256).Hash.ToLowerInvariant();runtimeSources=$presenceFrozen.runtimeSources;cloneAssetsMatched=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process.json') -Raw
