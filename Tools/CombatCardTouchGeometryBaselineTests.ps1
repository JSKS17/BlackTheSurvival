$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'CombatCardTouchSourceHelpers.ps1')
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parent=Join-Path $workspace 'Temp/CombatCardTouchGeometryBaseline'
$temporary=[IO.Path]::GetFullPath((Join-Path $parent ([guid]::NewGuid().ToString('N'))))
$scope=[IO.Path]::GetFullPath($parent)+[IO.Path]::DirectorySeparatorChar
if(-not $temporary.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid geometry baseline scope'}
[IO.Directory]::CreateDirectory($temporary)|Out-Null
try {
    $source=Join-Path $temporary 'LumiaTouchScroll.cs'
    $content=Get-CombatCardTouchBeforeGeometry ([IO.File]::ReadAllText((Join-Path $workspace 'Assets/Scripts/Lumia/LumiaTouchScroll.cs')))
    $expected='6dec57870b95cb38bbb89591cab6b9e3c5afa917e7d8b27c756706fd25b7f867'
    Write-CombatCardTouchExactSnapshot $content $source $expected
    & (Join-Path $PSScriptRoot 'CombatCardTouchTests.ps1') -TouchSource $source -EvidenceName 'combat-card-touch-geometry-baseline-tests.json'
    if($LASTEXITCODE -eq 0){throw 'Before-geometry patch must fail nested mobile viewport regression'}
    Write-Output ('Verified before-geometry source SHA '+$expected)
} finally {
    $resolved=[IO.Path]::GetFullPath($temporary)
    if(-not $resolved.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing cleanup outside geometry baseline scope'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
    if((Test-Path -LiteralPath $parent)-and @(Get-ChildItem -LiteralPath $parent -Force).Count -eq 0){Remove-Item -LiteralPath $parent -Force}
}
