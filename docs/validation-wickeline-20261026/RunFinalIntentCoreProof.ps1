#requires -Version 7.0
$ErrorActionPreference='Stop'
$wickRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-Location -LiteralPath $wickRoot
$wickFrozen=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-validation.json') -Raw | ConvertFrom-Json -AsHashtable
$wickPaths=@(@($wickFrozen.files.Keys | Where-Object {$_ -match '^Assets/Scripts/Lumia/[^/]+\.cs$'}) + @($wickFrozen.testSourceHashes.Keys) | Sort-Object -Unique)
$wickEntries=@(foreach($wickPath in $wickPaths){
    $wickActual=Join-Path $wickRoot $wickPath
    $wickHash=(Get-FileHash -LiteralPath $wickActual -Algorithm SHA256).Hash.ToLowerInvariant()
    $wickExpected=if($wickFrozen.testSourceHashes.ContainsKey($wickPath)){$wickFrozen.testSourceHashes[$wickPath]}else{$wickFrozen.files[$wickPath]}
    if($wickHash -ne $wickExpected){throw ('Test input differs from freeze: '+$wickPath)}
    [ordered]@{path=$wickPath;bytes=(Get-Item -LiteralPath $wickActual).Length;sha256=$wickHash}
})
[ordered]@{createdAtUtc=[DateTime]::UtcNow.ToString('o');files=$wickEntries} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'source-manifest.json') -Encoding utf8
$wickLog=Join-Path $PSScriptRoot 'core-tests.log'
'START_UTC='+[DateTime]::UtcNow.ToString('o') | Set-Content -LiteralPath $wickLog -Encoding utf8
& (Join-Path $wickRoot 'Tools/RunCoreTests.ps1') *>&1 | Tee-Object -FilePath $wickLog -Append
$wickExit=$LASTEXITCODE
'END_UTC='+[DateTime]::UtcNow.ToString('o') | Add-Content -LiteralPath $wickLog -Encoding utf8
'EXIT_CODE='+$wickExit | Add-Content -LiteralPath $wickLog -Encoding utf8
if($wickExit -ne 0){exit $wickExit}
& node (Join-Path $PSScriptRoot 'RecordCoreResults.mjs')
exit $LASTEXITCODE
