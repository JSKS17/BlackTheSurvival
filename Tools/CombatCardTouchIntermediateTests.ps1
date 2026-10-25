$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'CombatCardTouchSourceHelpers.ps1')
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parent=Join-Path $workspace 'Temp/CombatCardTouchIntermediate'
$temporary=[IO.Path]::GetFullPath((Join-Path $parent ([guid]::NewGuid().ToString('N'))))
$scope=[IO.Path]::GetFullPath($parent)+[IO.Path]::DirectorySeparatorChar
if(-not $temporary.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid intermediate test scope'}
[IO.Directory]::CreateDirectory($temporary)|Out-Null
try {
    $source=Join-Path $temporary 'LumiaTouchScroll.cs'
    $content=Get-CombatCardTouchBeforeGeometry ([IO.File]::ReadAllText((Join-Path $workspace 'Assets/Scripts/Lumia/LumiaTouchScroll.cs')))
    $before=@'
                && previousControl == 0 && GUIUtility.hotControl != 0)
            { touchButtonControl = GUIUtility.hotControl; touchEndedFrame = -1; }
'@
    $after='                && previousControl == 0 && GUIUtility.hotControl != 0) touchButtonControl = GUIUtility.hotControl;'
    $before=$before.Replace("`r`n","`n")
    if(-not $content.Contains($before)){throw 'Marker-reset change was not found'}
    $content=$content.Replace($before,$after)
    $expected='dc19701fe035320bd7094542bf9a907d09b2ba6c1ab2f4332560eb34c0a93312'
    $matched=$false
    foreach($newline in @("`n","`r`n")){
        foreach($bom in @($false,$true)){
            [IO.File]::WriteAllText($source,$content.Replace("`n",$newline),[Text.UTF8Encoding]::new($bom))
            if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -eq $expected){$matched=$true;break}
        }
        if($matched){break}
    }
    if(-not $matched){throw 'Intermediate source reconstruction did not match the exact earlier-patch SHA'}
    & (Join-Path $PSScriptRoot 'CombatCardTouchTests.ps1') -TouchSource $source -EvidenceName 'combat-card-touch-intermediate-tests.json'
    if($LASTEXITCODE -eq 0){throw 'Intermediate patch must fail the ordinary desktop click regression'}
    $result=Get-Content -LiteralPath (Join-Path $workspace 'docs/validation-combat-card-scroll-20261022/combat-card-touch-intermediate-tests.json') -Raw|ConvertFrom-Json
    if($result.result -ne 'FAIL' -or $result.scenarios -ne 23 -or @($result.failedScenarios).Count -ne 7 -or -not @($result.failedScenarios).Contains('DesktopClickAfterEarlierResizeCancel')){throw 'Unexpected intermediate patch regression result'}
    Write-Output ('Verified intermediate source SHA '+$expected+'; desktop click regression reproduced')
} finally {
    $resolved=[IO.Path]::GetFullPath($temporary)
    if(-not $resolved.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing cleanup outside intermediate test scope'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
    if((Test-Path -LiteralPath $parent)-and @(Get-ChildItem -LiteralPath $parent -Force).Count -eq 0){Remove-Item -LiteralPath $parent -Force}
}
