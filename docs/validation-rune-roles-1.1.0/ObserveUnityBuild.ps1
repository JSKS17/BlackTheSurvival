#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$presenceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$presenceStart = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-process.json') -Raw | ConvertFrom-Json
$presenceLog = Join-Path $PSScriptRoot 'unity-build.log'
$presenceClone = Join-Path $presenceRoot 'Build/UnityWebBuild'
$presencePlayer = Join-Path $presenceRoot 'Build/Web110'
$presenceTimeline = Join-Path $PSScriptRoot 'unity-build-observations.jsonl'
$presenceProcess = Get-Process -Id $presenceStart.processId
$null = $presenceProcess.Handle
$presenceObservedAt = [DateTime]::UtcNow.ToString('o')

function Read-PresenceBuildState {
    $presenceText = Get-Content -LiteralPath $presenceLog -Raw
    $presenceTail = ($presenceText -split '\r?\n' | Select-Object -Last 150) -join "`n"
    $presencePhase = if ($presenceText -match "Build completed with a result of 'Succeeded'") { 'Succeeded' }
        elseif ($presenceTail -match 'wasm-opt') { 'wasm-opt' }
        elseif ($presenceTail -match 'Emscripten|emcc|Link_WebGL') { 'WebGL native compile or link' }
        elseif ($presenceText -match 'IL2CPP|il2cpp') { 'IL2CPP conversion or native compile' }
        elseif ($presenceText -match 'Build Player|Building Player|Preparing build|Player build') { 'Preparing player build' }
        elseif ($presenceText -match 'Start importing|Importing|Refresh') { 'Importing or refreshing assets' }
        else { 'Editor startup' }
    $presenceErrors = ([regex]::Matches($presenceText, "(?im)(?:error CS\d+|BuildFailedException|Build completed with a result of 'Failed'|Fatal error|Error building Player)")).Count
    $presenceObjects = @(Get-ChildItem -LiteralPath (Join-Path $presenceClone 'Library/Bee') -Recurse -File -Filter '*.o' -ErrorAction SilentlyContinue).Count
    $presenceProcess.Refresh()
    $presenceState = [ordered]@{
        atUtc = [DateTime]::UtcNow.ToString('o')
        processId = $presenceStart.processId
        processRunning = !$presenceProcess.HasExited
        phase = $presencePhase
        nativeObjectFiles = $presenceObjects
        errorMarkers = $presenceErrors
        logBytes = (Get-Item -LiteralPath $presenceLog).Length
    }
    $presenceJson = $presenceState | ConvertTo-Json -Compress
    Add-Content -LiteralPath $presenceTimeline -Value $presenceJson -Encoding UTF8
    Write-Output $presenceJson
}

Read-PresenceBuildState
while (!$presenceProcess.WaitForExit(45000)) { Read-PresenceBuildState }
$presenceProcess.WaitForExit()
Read-PresenceBuildState
$presenceFreezePath = Join-Path $PSScriptRoot 'source-validation.json'
$presenceFreeze = Get-Content -LiteralPath $presenceFreezePath -Raw | ConvertFrom-Json -AsHashtable
$presenceMismatch = [System.Collections.Generic.List[string]]::new()
$presenceChecked = 0
foreach ($presenceMap in @($presenceFreeze.product, $presenceFreeze.unchangedArt)) {
    foreach ($presenceEntry in $presenceMap.GetEnumerator()) {
        if (!$presenceEntry.Key.StartsWith('Assets/')) { continue }
        $presenceChecked++
        $presenceSource = Join-Path $presenceRoot $presenceEntry.Key
        $presenceCopy = Join-Path $presenceClone $presenceEntry.Key
        if (!(Test-Path -LiteralPath $presenceCopy) -or
            (Get-FileHash -LiteralPath $presenceCopy -Algorithm SHA256).Hash.ToLowerInvariant() -ne $presenceEntry.Value -or
            (Get-FileHash -LiteralPath $presenceSource -Algorithm SHA256).Hash.ToLowerInvariant() -ne $presenceEntry.Value) {
            $presenceMismatch.Add($presenceEntry.Key)
        }
    }
}
$presenceManifestPath = Join-Path $presencePlayer 'build-manifest.json'
$presenceManifest = if (Test-Path -LiteralPath $presenceManifestPath) { Get-Content -LiteralPath $presenceManifestPath -Raw | ConvertFrom-Json } else { $null }
$presenceSuccess = $presenceProcess.ExitCode -eq 0 -and $presenceManifest -and $presenceManifest.result -eq 'Succeeded' -and
    $presenceManifest.productVersion -eq '1.1.0' -and $presenceManifest.errors -eq 0 -and $presenceMismatch.Count -eq 0
$presenceResult = [ordered]@{
    result = if ($presenceSuccess) { 'PASS' } else { 'FAIL' }
    processId = $presenceStart.processId
    observerStartedAtUtc = $presenceObservedAt
    finishedAtUtc = [DateTime]::UtcNow.ToString('o')
    exitCode = $presenceProcess.ExitCode
    manifestResult = $presenceManifest.result
    productVersion = $presenceManifest.productVersion
    errors = $presenceManifest.errors
    warnings = $presenceManifest.warnings
    durationSeconds = $presenceManifest.durationSeconds
    checkedAssetFiles = $presenceChecked
    rootAndCloneMatchFreeze = $presenceMismatch.Count -eq 0
    mismatches = $presenceMismatch.ToArray()
    sourceFreezeSha256 = (Get-FileHash -LiteralPath $presenceFreezePath -Algorithm SHA256).Hash.ToLowerInvariant()
    manifestSha256 = if ($presenceManifest) { (Get-FileHash -LiteralPath $presenceManifestPath -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
}
$presenceResult | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'unity-build-observer-result.json') -Encoding UTF8
$presenceResult | ConvertTo-Json -Depth 5 -Compress
if (!$presenceSuccess) { exit 1 }
