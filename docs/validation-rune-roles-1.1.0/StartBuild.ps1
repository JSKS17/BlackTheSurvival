#requires -Version 7.0
$ErrorActionPreference='Stop'
$runeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location -LiteralPath $runeRoot
$runeClone=Join-Path $runeRoot 'Build\UnityWebBuild'
$runeImport=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'import-process.json') -Raw | ConvertFrom-Json
while(Get-Process -Id $runeImport.processId -ErrorAction SilentlyContinue){
 [ordered]@{phase='Importing WebGL assets';imports=(Select-String -LiteralPath (Join-Path $PSScriptRoot 'unity-import.log') -Pattern '^Start importing').Count;atUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json -Compress
 Start-Sleep -Seconds 30
}
if(@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($runeClone,[StringComparison]::OrdinalIgnoreCase)}).Count){throw 'Owned Unity clone still open.'}
$runeFreezePath=Join-Path $PSScriptRoot 'source-validation.json'
$runeFrozen=Get-Content -LiteralPath $runeFreezePath -Raw | ConvertFrom-Json -AsHashtable
if($runeFrozen.result -ne 'PASS' -or $runeFrozen.version -ne '1.1.0'){throw 'Final 1.1.0 source freeze required.'}
foreach($runeMap in @($runeFrozen.product,$runeFrozen.unchangedArt,$runeFrozen.tools,$runeFrozen.sourceDocumentation)){foreach($runeEntry in $runeMap.GetEnumerator()){if((Get-FileHash -LiteralPath (Join-Path $runeRoot $runeEntry.Key) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $runeEntry.Value){throw ('Frozen root changed: '+$runeEntry.Key)}}}
if(Test-Path -LiteralPath (Join-Path $runeRoot 'Build\Web110')){throw 'Inspect and archive any previous owned Web110 output before starting another build.'}
& (Join-Path $runeRoot 'Tools\BuildWeb.ps1') -PrepareOnly -LowMemory -OutputDirectory Build/Web110 | Out-Null
foreach($runeMap in @($runeFrozen.product,$runeFrozen.unchangedArt)){foreach($runeEntry in $runeMap.GetEnumerator()){if($runeEntry.Key.StartsWith('Assets/')){if((Get-FileHash -LiteralPath (Join-Path $runeClone $runeEntry.Key) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $runeEntry.Value){throw ('Cloned source changed: '+$runeEntry.Key)}}}}
$runeLog=Join-Path $PSScriptRoot 'unity-build.log'
$runeArgs=@('-batchmode','-nographics','-buildTarget','WebGL','-projectPath',$runeClone,'-executeMethod','Lumia.Editor.LumiaWebBuild.Build','-logFile',$runeLog,'-lumia-web-output',(Join-Path $runeRoot 'Build\Web110'),'-lumia-web-compression','Gzip','-job-worker-count','1','-import-worker-count','1')
$runePrevious=[Environment]::GetEnvironmentVariable('BEE_BUILD_THREADS','Process')
try{[Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS','1','Process');$runeBuild=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe' -ArgumentList $runeArgs -WindowStyle Hidden -PassThru}
finally{[Environment]::SetEnvironmentVariable('BEE_BUILD_THREADS',$runePrevious,'Process')}
[ordered]@{processId=$runeBuild.Id;startedAtUtc=$runeBuild.StartTime.ToUniversalTime().ToString('o');log=$runeLog;sourceFreezeSha256=(Get-FileHash -LiteralPath $runeFreezePath -Algorithm SHA256).Hash.ToLowerInvariant();runtimeSources=$runeFrozen.runtimeSources;cloneAssetsMatched=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process.json') -Raw
