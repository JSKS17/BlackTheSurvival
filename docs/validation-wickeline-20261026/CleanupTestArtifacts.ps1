#requires -Version 7.0
$ErrorActionPreference='Stop'
$wickRoot='C:\Users\azxar\BlackTheSurvival'
$wickCore=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'core-tests.json') -Raw | ConvertFrom-Json
if($wickCore.result -ne 'PASS' -or $wickCore.exitCode -ne 0) {throw 'Successful final tests required.'}
if(Get-CimInstance Win32_Process | Where-Object {$_.Name -eq 'CoreTests.exe'}) {throw 'Tests still running.'}
$wickRemoved=@()
foreach($wickRelative in @('Temp\CoreTests.exe','Temp\LumiResearchTests','Temp\ModeUnlockTests','Temp\ResearchStoreTests','Temp\StoryHistoryTests')) {
    $wickPath=[IO.Path]::GetFullPath((Join-Path $wickRoot $wickRelative))
    if(-not $wickPath.StartsWith($wickRoot+'\Temp\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Outside owned test outputs.'}
    $wickItem=Get-Item -LiteralPath $wickPath -Force
    if($wickItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Linked test output.'}
    if($wickItem.PSIsContainer) {
        if(@(Get-ChildItem -LiteralPath $wickPath -Force).Count) {throw 'Expected empty completed-test directory.'}
        Remove-Item -LiteralPath $wickPath
        $wickRemoved+=@{path=$wickPath;bytes=0}
    } else {
        $wickRemoved+=@{path=$wickPath;bytes=$wickItem.Length;sha256=(Get-FileHash -LiteralPath $wickPath -Algorithm SHA256).Hash.ToLowerInvariant()}
        Remove-Item -LiteralPath $wickPath
    }
}
@{result='PASS';removedAtUtc=[DateTime]::UtcNow.ToString('o');removed=$wickRemoved;sourceAndProofsPreserved=$true} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'test-artifact-cleanup.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'test-artifact-cleanup.json') -Raw
