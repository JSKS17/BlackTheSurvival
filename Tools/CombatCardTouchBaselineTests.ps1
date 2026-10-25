$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'CombatCardTouchSourceHelpers.ps1')
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parent=Join-Path $workspace 'Temp/CombatCardTouchBaseline'
$temporary=[IO.Path]::GetFullPath((Join-Path $parent ([guid]::NewGuid().ToString('N'))))
$scope=[IO.Path]::GetFullPath($parent)+[IO.Path]::DirectorySeparatorChar
if(-not $temporary.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid baseline reconstruction scope'}
[IO.Directory]::CreateDirectory($temporary)|Out-Null
$baseline=Join-Path $temporary 'LumiaTouchScroll.cs'
$report=Get-Content -LiteralPath (Join-Path $workspace 'docs/validation-mobile-fonts-20261022/source-validation.json') -Raw|ConvertFrom-Json
$expected=$report.runtimeSourceHashes.'LumiaTouchScroll.cs'
try {
    $script:content=Get-CombatCardTouchBeforeGeometry ([IO.File]::ReadAllText((Join-Path $workspace 'Assets/Scripts/Lumia/LumiaTouchScroll.cs')))
    function Reverse([string]$from,[string]$to){
        $from=$from.Replace("`r`n","`n");$to=$to.Replace("`r`n","`n")
        if(-not $script:content.Contains($from)){throw ('Reconstruction pattern missing: '+$from)}
        $script:content=$script:content.Replace($from,$to)
    }
    Reverse 'touchFinger, touchNativeFinger, touchButtonControl' 'touchFinger, touchButtonControl'
    Reverse 'touchNativeExists, touchNativeTracking;' 'touchNativeExists;'
    Reverse 'bool BeginNativeTouchScroll(int finger, Vector2 point, bool native = false)' 'bool BeginNativeTouchScroll(int finger, Vector2 point)'
    Reverse @'
            touchFinger = finger; touchNativeFinger = finger; touchNativeTracking = native;
            touchSuppressUntil = -1; touchReleaseButton = false; touchEndedFrame = -1;
'@ @'
            touchFinger = finger; touchSuppressUntil = -1; touchReleaseButton = false; touchEndedFrame = -1;
'@
    Reverse 'touchScrollGesture.Cancel(); touchNativeTracking = false; touchEndedFrame = Time.frameCount;' 'touchScrollGesture.Cancel(); touchEndedFrame = Time.frameCount;'
    Reverse '{ touchScrollGesture.Reset(); touchNativeTracking = false; }' '    touchScrollGesture.Reset();'
    Reverse @'
            // Browser GUI events can arrive before legacy Input's touch snapshot.
            // Adopt that snapshot without changing the gesture's stable pointer ID.
            if (touchScrollGesture.IsTracking && !touchNativeTracking)
            { touchNativeTracking = true; touchNativeFinger = touch.fingerId; }
'@ ''
    # Removing the block must also remove its now-empty line.
    $script:content=$script:content.Replace("`n`n            if (touch.phase == TouchPhase.Canceled)","`n            if (touch.phase == TouchPhase.Canceled)")
    Reverse 'BeginNativeTouchScroll(touch.fingerId, touchNativePoint, true)' 'BeginNativeTouchScroll(touch.fingerId, touchNativePoint)'
    Reverse 'touch.fingerId != touchNativeFinger' 'touch.fingerId != touchFinger'
    Reverse '(!touchNativeExists || touchNativePhase != TouchPhase.Canceled)' 'touchNativePhase != TouchPhase.Canceled'
    Reverse 'BeginNativeTouchScroll(touchNativeExists ? Input.GetTouch(0).fingerId : touchFinger, eventPoint, touchNativeExists)' 'BeginNativeTouchScroll(touchNativeExists ? Input.GetTouch(0).fingerId : touchFinger, eventPoint)'
    Reverse @'
            // An event-only Touch pointer has no polled native position or release.
            // Keep it alive until GUI Up instead of ending it at a stale (0, 0).
'@ ''
    $script:content=$script:content.Replace("`n`n            if (touchRawGuiEvent == EventType.Repaint", "`n            if (touchRawGuiEvent == EventType.Repaint")
    Reverse 'touchScrollGesture.IsTracking && touchNativeTracking && !touchWaitForRelease' 'touchScrollGesture.IsTracking && !touchWaitForRelease'
    Reverse @'
            // Remember our own button even when GUI Down precedes Input.Began.
            // Otherwise the next Layout mistakes that card for a foreign control
            // and cancels its pending swipe before it crosses the threshold.
            if ((eventType == EventType.MouseDown || eventType == EventType.TouchDown)
                && previousControl == 0 && GUIUtility.hotControl != 0)
            { touchButtonControl = GUIUtility.hotControl; touchEndedFrame = -1; }
            if ((eventType == EventType.MouseUp || eventType == EventType.TouchUp) && GUIUtility.hotControl == 0) touchButtonControl = 0;
'@ @'
            if (eventType == EventType.MouseDown && touchScrollGesture.IsTracking && !touchScrollGesture.IsDragging
                && previousControl == 0 && GUIUtility.hotControl != 0) touchButtonControl = GUIUtility.hotControl;
            if (eventType == EventType.MouseUp && GUIUtility.hotControl == 0) touchButtonControl = 0;
'@
    $matched=$false
    foreach($newline in @("`n","`r`n")){
        foreach($bom in @($false,$true)){
            [IO.File]::WriteAllText($baseline,$script:content.Replace("`n",$newline),[Text.UTF8Encoding]::new($bom))
            $actual=(Get-FileHash -LiteralPath $baseline -Algorithm SHA256).Hash.ToLowerInvariant()
            if($actual -eq $expected){$matched=$true;break}
        }
        if($matched){break}
    }
    if(-not $matched){throw ('Baseline reconstruction SHA does not match original: expected '+$expected+' actual '+$actual)}
    & (Join-Path $PSScriptRoot 'CombatCardTouchTests.ps1') -TouchSource $baseline -EvidenceName 'combat-card-touch-baseline-tests.json'
    if($LASTEXITCODE -eq 0){throw 'Previous production must fail the new native integration cases'}
    $evidence=Join-Path $workspace 'docs/validation-combat-card-scroll-20261022/combat-card-touch-baseline-tests.json'
    $result=Get-Content -LiteralPath $evidence -Raw|ConvertFrom-Json
    if($result.result -ne 'FAIL' -or @($result.failedScenarios).Count -ne 11 -or $result.scenarios -ne 23){throw 'Baseline did not report the expected behavioral failures'}
    Write-Output ('Verified original production SHA '+$expected+'; regression failures: '+($result.failedScenarios -join ', '))
} finally {
    $resolved=[IO.Path]::GetFullPath($temporary)
    if(-not $resolved.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Refusing cleanup outside baseline test scope'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
    if((Test-Path -LiteralPath $parent)-and @(Get-ChildItem -LiteralPath $parent -Force).Count -eq 0){Remove-Item -LiteralPath $parent -Force}
}
