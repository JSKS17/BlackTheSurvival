#requires -Version 7.0
$ErrorActionPreference='Stop'
$wickRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$wickClone=Join-Path $wickRoot 'Build\UnityWebBuild'
$wickPlayer=Join-Path $wickClone 'Build\Windows'
$wickBuildManifest=Join-Path $wickClone 'Build\windows-build-manifest.json'
$wickManifest=Get-Content -LiteralPath $wickBuildManifest -Raw | ConvertFrom-Json
$wickFreezeSha=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'source-validation.json') -Algorithm SHA256).Hash.ToLowerInvariant()
if($wickManifest.result -ne 'Succeeded' -or $wickManifest.productVersion -ne '1.0.27' -or $wickManifest.errors -ne 0 -or $wickManifest.development -ne $false -or $wickManifest.sourceCaptureSha256 -ne $wickFreezeSha) {throw 'Windows production build/source evidence mismatch.'}
foreach($wickRelative in @('BlackTheSurvival.exe','UnityPlayer.dll','BlackTheSurvival_Data','MonoBleedingEdge')) {
    if(-not (Test-Path -LiteralPath (Join-Path $wickPlayer $wickRelative))) {throw ('Incomplete player: '+$wickRelative)}
}
$wickRelease=Join-Path $wickRoot 'Temp\WickelineRelease20261026'
if(-not (Test-Path -LiteralPath $wickRelease -PathType Container)) {throw 'PrepareRelease prepare must create the owned release staging directory first.'}
$wickZip=Join-Path $wickRelease 'BlackTheSurvival-v1.0.27-Windows-x64.zip'
if(Test-Path -LiteralPath $wickZip) {throw 'Refusing to replace an existing Windows release ZIP.'}
[IO.Compression.ZipFile]::CreateFromDirectory($wickPlayer,$wickZip,[IO.Compression.CompressionLevel]::Optimal,$false)
$wickSha=(Get-FileHash -LiteralPath $wickZip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $wickRelease 'SHA256SUMS-v1.0.27.txt'),$wickSha+'  '+[IO.Path]::GetFileName($wickZip)+"`n",[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath $wickBuildManifest -Destination (Join-Path $PSScriptRoot 'windows-build-manifest.json')
[ordered]@{result='PASS';version='1.0.27';createdAtUtc=[DateTime]::UtcNow.ToString('o');zip=$wickZip;bytes=(Get-Item -LiteralPath $wickZip).Length;sha256=$wickSha;sourceFreezeSha256=$wickFreezeSha;manifestSha256=(Get-FileHash -LiteralPath $wickBuildManifest -Algorithm SHA256).Hash.ToLowerInvariant()} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'windows-package.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'windows-package.json') -Raw
