#requires -Version 7.0
$ErrorActionPreference='Stop'
$wickRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).ProviderPath.TrimEnd('\')
$wickClone=Join-Path $wickRoot 'Build\UnityWebBuild'
$wickTarget=[IO.Path]::GetFullPath((Join-Path $wickClone 'Library\Bee'))
if(-not $wickTarget.StartsWith($wickRoot+'\Build\UnityWebBuild\Library\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Invalid task cache path.'}
$wickBrowser=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'browser-validation.json') -Raw | ConvertFrom-Json
$wickManifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'unity-build-manifest.json') -Raw | ConvertFrom-Json
if($wickBrowser.result -ne 'PASS' -or $wickBrowser.gameVersion -ne '1.0.27' -or $wickManifest.result -ne 'Succeeded' -or $wickManifest.productVersion -ne '1.0.27') {throw 'Web build and browser verification must pass before discarding rebuild caches.'}
if((Get-FileHash -LiteralPath (Join-Path $wickRoot 'Build\Web127\build-manifest.json')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'unity-build-manifest.json')).Hash) {throw 'Raw output ownership mismatch.'}
if(@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($wickClone,[StringComparison]::OrdinalIgnoreCase)}).Count) {throw 'Unity must exit before trimming its owned build cache.'}
$wickCursor=$wickTarget
while(-not $wickCursor.Equals($wickRoot,[StringComparison]::OrdinalIgnoreCase)) {
    if((Get-Item -LiteralPath $wickCursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Linked cache path refused.'}
    $wickCursor=[IO.Path]::GetDirectoryName($wickCursor)
}
$wickEntries=@(Get-ChildItem -LiteralPath $wickTarget -Recurse -Force)
if($wickEntries | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}) {throw 'Linked cache child refused.'}
$wickBytes=[long](($wickEntries | Where-Object {-not $_.PSIsContainer} | Measure-Object Length -Sum).Sum)
Remove-Item -LiteralPath $wickTarget -Recurse -Force
if(Test-Path -LiteralPath $wickTarget) {throw 'Owned cache remains.'}
[ordered]@{result='PASS';removedAtUtc=[DateTime]::UtcNow.ToString('o');path=$wickTarget;removedBytes=$wickBytes;reason='Web build and browser QA passed; discard WebGL native intermediates before reusing source/assets for the Windows Mono build.';rootSourcesPreserved=$true;rawWebBuildPreserved=$true;noProcessWasStopped=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'cache-trim.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cache-trim.json') -Raw
