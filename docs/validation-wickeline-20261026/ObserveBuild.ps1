#requires -Version 7.0
$ErrorActionPreference='Stop'
$wickRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$wickRecordRaw=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process-web.json') -Raw
$wickRecord=$wickRecordRaw | ConvertFrom-Json
$wickStart=[DateTimeOffset]::Parse([regex]::Match($wickRecordRaw,'"startedAtUtc":\s*"([^"]+)"').Groups[1].Value).UtcDateTime
while($true) {
    $wickProcess=Get-Process -Id $wickRecord.processId -ErrorAction SilentlyContinue
    if(-not $wickProcess -or [Math]::Abs(($wickProcess.StartTime.ToUniversalTime()-$wickStart).TotalSeconds) -gt 1) {break}
    $wickObjects=@(Get-ChildItem -LiteralPath (Join-Path $wickRoot 'Build\UnityWebBuild\Library\Bee\artifacts') -Recurse -File -Filter '*.o' -ErrorAction SilentlyContinue).Count
    [ordered]@{atUtc=[DateTime]::UtcNow.ToString('o');phase='Web compilation';objects=$wickObjects;unityCpuSeconds=[Math]::Round($wickProcess.CPU,1);freeBytes=(Get-PSDrive -Name C).Free} | ConvertTo-Json -Compress
    Start-Sleep -Seconds 45
}
[ordered]@{atUtc=[DateTime]::UtcNow.ToString('o');phase='Unity exited';manifestPresent=(Test-Path -LiteralPath (Join-Path $wickRoot 'Build\Web127\build-manifest.json'))} | ConvertTo-Json -Compress
