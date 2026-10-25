[CmdletBinding()]
param([switch] $VerifiedServersStopped)

$ErrorActionPreference = 'Stop'
if (-not $VerifiedServersStopped) { throw 'Stop this task''s local preview/test/build servers before running Cleanup.ps1 -VerifiedServersStopped.' }
$taskRoot = (Resolve-Path -LiteralPath 'C:\Users\azxar\BlackTheSurvival').ProviderPath.TrimEnd('\')
$proofRoot = Join-Path $taskRoot 'docs\validation-save-concurrency-20261022'
$stagePath = Join-Path $taskRoot 'Temp\SiteSaveConcurrency20261022'
$canonicalModules = Join-Path $taskRoot 'WebSite\node_modules'
$reportPath = Join-Path $proofRoot 'cleanup.json'
$ordinal = [StringComparison]::OrdinalIgnoreCase

function Assert-WorkspacePath([string] $path) {
    $absolute = [IO.Path]::GetFullPath($path).TrimEnd('\')
    if (-not $absolute.StartsWith($taskRoot + '\', $ordinal)) { throw "Target escaped the workspace: $absolute" }
    # Reject a linked parent before any operation can resolve through it.
    $parent = [IO.Path]::GetDirectoryName($absolute)
    while ($parent -and -not $parent.Equals($taskRoot, $ordinal)) {
        if (-not $parent.StartsWith($taskRoot + '\', $ordinal)) { throw 'Unexpected ancestor outside workspace.' }
        if ([IO.Directory]::Exists($parent) -and ([IO.File]::GetAttributes($parent) -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked ancestor is not allowed: $parent" }
        $parent = [IO.Path]::GetDirectoryName($parent)
    }
    return $absolute
}

function Read-Proof([string] $name) {
    $path = Assert-WorkspacePath (Join-Path $proofRoot $name)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing required proof: $name" }
    if ([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) { throw "Proof must be an ordinary file: $name" }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Assert-PassingProof([string] $name) {
    $value = Read-Proof $name
    $passed = $value.result -eq 'PASS' -or $value.status -eq 'passed'
    if (-not $passed -and $value.tests) { $passed = $value.tests.passed -gt 0 -and $value.tests.failed -eq 0 }
    if (-not $passed) { throw "Required validation has not passed: $name" }
    return $value
}

$publish = Read-Proof 'publish-state.json'
$live = Read-Proof 'live-validation.json'
$canonical = Read-Proof 'canonical-sync.json'
$null = Assert-PassingProof 'backend-validation.json'
$null = Assert-PassingProof 'frontend-validation.json'
if ($publish.deployment.status -ne 'succeeded') { throw 'Publication must succeed before cleanup.' }
if ($live.result -ne 'PASS') { throw 'Public verification must pass before cleanup.' }
if (-not $canonical.allFilesVerified) { throw 'Canonical source must be synchronized and verified before cleanup.' }

# These are source/dependency assets, even where a directory happens to be named build.
$preservedRoots = @('Assets','ArtSource','Packages','ProjectSettings','Tools','docs','WebSite\build','WebSite\public\game\Build','WebSite\.sites-runtime','WebSite\node_modules')
foreach ($relative in $preservedRoots) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $relative) -PathType Container)) { throw "Required source/dependency folder is missing: $relative" }
}

# Inspect, but never stop, processes. Running servers must be stopped by their owner.
# CIM requires an elevated read on hosts where ordinary sandbox access is restricted.
$processes = Get-CimInstance Win32_Process -ErrorAction Stop
$recognizedExecutables = @('CoreTests.exe','ExportTacticalPreview.exe','ProseAudit.exe','TacticalAdvancedSmokeTests.exe','TacticalModuleTests.exe','TacticalQaFullTail20261020.exe','TacticalStatusSmokeTests.exe')
$runningOwned = @($processes | Where-Object {
    $cmd = [string]$_.CommandLine
    $exe = [string]$_.ExecutablePath
    ($_.Name -in @('Unity.exe','bee_backend.exe','node.exe','workerd.exe','mono.exe','dotnet.exe') -and $cmd.IndexOf($taskRoot, $ordinal) -ge 0) -or
    ($_.Name -in $recognizedExecutables -and $exe.StartsWith((Join-Path $taskRoot 'Temp') + '\', $ordinal))
})
if ($runningOwned.Count) { throw ('Task-related processes are still running; refusing cleanup. PIDs: ' + (($runningOwned | ForEach-Object { $_.ProcessId }) -join ', ')) }

# Exact whitelist only: never recursively erase all of Temp.
$oldTempLeaves = @(
    'DescriptionAudit','LumiaVerification','ModeUnlockTests','ResearchStoreTests','StoryHistoryTests','TacticalPreview',
    'CoreTests.exe','ExportTacticalPreview.exe','ProseAudit.exe','TacticalAdvancedSmokeTests.exe','TacticalModuleTests.exe','TacticalStatusSmokeTests.exe',
    'RunTacticalModuleTests.ps1','TacticalModuleCoreSmokeTests.cs','TacticalModuleTestRunner.cs','TacticalStatusHarness.cs',
    'TacticalQaFullTail20261020-result.json','TacticalQaFullTail20261020-resumed.log','TacticalQaFullTail20261020.cs','TacticalQaFullTail20261020.exe','TacticalQaFullTail20261020.log','TacticalQaFullTail20261020.methods.txt',
    'release119-full-core-tests.log','release119-summary-tests.log','release119-tactical-tests.log',
    'release120-core-tests.log','release120-description-audit-final.log','release120-description-audit-updated.log','release120-description-audit.log',
    'sua-replay-core-tests.log','sua-replay-summary-tests.log','tactical-full-core-tests.log','tactical-prose-summary-tests.log','tactical-prose-tests.log','tactical-runtime-compile.log','tactical-weapon-regression.log'
)
$targetSpecs = [Collections.Generic.List[object]]::new()
$targetSpecs.Add([pscustomobject]@{ relative='Build'; kind='All root build exports, archives, caches and obsolete verification outputs' })
foreach ($leaf in $oldTempLeaves) { $targetSpecs.Add([pscustomobject]@{ relative=('Temp\' + $leaf); kind='Recognized obsolete generated test/preview artifact' }) }
$targetSpecs.Add([pscustomobject]@{ relative='Temp\SiteSaveConcurrency20261022'; kind='This completed publication checkout and local QA data' })
foreach ($relative in @('WebSite\dist','WebSite\.next','WebSite\.wrangler')) { $targetSpecs.Add([pscustomobject]@{ relative=$relative; kind='Canonical generated web build/preview output' }) }
foreach ($leaf in @('site.tar.gz','site-final.tar.gz')) { $targetSpecs.Add([pscustomobject]@{ relative=('docs\validation-save-concurrency-20261022\' + $leaf); kind='Saved publication archive' }) }

$allowedLinks = @(
    (Join-Path $taskRoot 'Build\SiteTactical118\node_modules'),
    (Join-Path $stagePath 'node_modules')
)
$linksFound = [Collections.Generic.List[string]]::new()
function Assert-AllowedJunction([string] $path) {
    $absolute = Assert-WorkspacePath $path
    if (-not ($allowedLinks | Where-Object { $_.Equals($absolute, $ordinal) })) { throw "Unexpected linked cleanup target: $absolute" }
    $item = Get-Item -LiteralPath $absolute -Force
    $targets = @($item.Target)
    if ($item.LinkType -ne 'Junction' -or $targets.Count -ne 1 -or -not ([IO.Path]::GetFullPath([string]$targets[0]).TrimEnd('\')).Equals($canonicalModules, $ordinal)) { throw "Unexpected dependency junction target: $absolute" }
    if (-not $linksFound.Contains($absolute)) { $linksFound.Add($absolute) }
}

function Measure-CheckedTree([string] $path, [bool] $permitKnownLinks = $true) {
    $absolute = Assert-WorkspacePath $path
    $totalBytes = [long]0
    $fileCount = [long]0
    $initial = Get-Item -LiteralPath $absolute -Force
    if ($initial.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Cleanup root must not be linked: $absolute" }
    if (-not $initial.PSIsContainer) { return [pscustomobject]@{ bytes=[long]$initial.Length; files=[long]1 } }
    $stack = [Collections.Generic.Stack[string]]::new()
    $stack.Push($absolute)
    while ($stack.Count -gt 0) {
        foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($stack.Pop())) {
            $attributes = [IO.File]::GetAttributes($entry)
            if ($attributes -band [IO.FileAttributes]::ReparsePoint) {
                if (-not $permitKnownLinks) { throw "A link appeared after preflight/unlink; refusing recursive cleanup: $entry" }
                Assert-AllowedJunction $entry; continue
            }
            if ($attributes -band [IO.FileAttributes]::Directory) { $stack.Push($entry) }
            else { $totalBytes += [IO.FileInfo]::new($entry).Length; $fileCount++ }
        }
    }
    return [pscustomobject]@{ bytes=$totalBytes; files=$fileCount }
}

# Resolve and inspect every target before any mutation. Reject new/unexpected links.
$targets = [Collections.Generic.List[object]]::new()
$missing = [Collections.Generic.List[string]]::new()
foreach ($spec in $targetSpecs) {
    $absolute = Assert-WorkspacePath (Join-Path $taskRoot $spec.relative)
    if (-not $absolute.Equals([IO.Path]::GetFullPath((Join-Path $taskRoot $spec.relative)).TrimEnd('\'), $ordinal)) { throw 'Target identity changed.' }
    if (-not (Test-Path -LiteralPath $absolute)) { $missing.Add($absolute); continue }
    $measured = Measure-CheckedTree $absolute
    $targets.Add([pscustomobject]@{ path=$absolute; bytes=$measured.bytes; files=$measured.files; kind=$spec.kind })
}

# Preserve only the specifically selected compact historical proof files.
$evidencePaths = @(
    'Build\LoginSaveDiagnosis20261021\diagnosis.json',
    'Build\LoginSaveDiagnosis20261021\global-save-lock-reproduction.json',
    'Build\LoginSaveDiagnosis20261021\reproduce-global-save-lock.mjs',
    'Build\DisplayPreferencesVerification\publish-state.json',
    'Build\DisplayPreferencesVerification\backend-validation.json',
    'Build\DisplayPreferencesVerification\client-validation.json',
    'Build\DisplayPreferencesVerification\integration-validation.json',
    'Build\DisplayPreferencesVerification\browser-validation.json',
    'Build\DisplayPreferencesVerification\live-validation.json',
    'Build\Release120Verification\Transition\state.json'
)
$backups = [Collections.Generic.List[object]]::new()
$skippedEvidence = [Collections.Generic.List[object]]::new()
foreach ($relative in $evidencePaths) {
    $source = Assert-WorkspacePath (Join-Path $taskRoot $relative)
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { $skippedEvidence.Add([pscustomobject]@{path=$source;reason='Already absent'}); continue }
    $item = Get-Item -LiteralPath $source -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Historical evidence cannot be linked.' }
    if ($item.Length -gt 262144) {
        if ($relative -eq 'Build\Release120Verification\Transition\state.json') { $skippedEvidence.Add([pscustomobject]@{path=$source;reason='Exceeds optional 256 KiB compact-proof limit'}); continue }
        throw "Selected evidence unexpectedly exceeds compact limit: $relative"
    }
    $destination = Assert-WorkspacePath (Join-Path (Join-Path $proofRoot 'prior-evidence') $relative)
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    if (Test-Path -LiteralPath $destination) {
        $existing = Get-Item -LiteralPath $destination -Force
        if ($existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) { throw "Existing backup differs: $destination" }
    } else {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -ErrorAction Stop
    }
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourceHash) { throw 'Evidence backup did not verify.' }
    $backups.Add([pscustomobject]@{ source=$source; destination=$destination; bytes=$item.Length; sha256=$sourceHash })
}

$freeBefore = [IO.DriveInfo]::new('C:\').AvailableFreeSpace
$removed = [Collections.Generic.List[object]]::new()
$record = [ordered]@{
    result='IN_PROGRESS'; startedAtUtc=[DateTime]::UtcNow.ToString('o'); workspace=$taskRoot
    gates=@('Deployment succeeded','Live verification PASS','Canonical allFilesVerified','Backend PASS','Frontend PASS','Owned servers stopped')
    processesInspected=$true; targets=$targets.ToArray(); unlinkedJunctions=@(); copiedEvidence=$backups.ToArray(); skippedEvidence=$skippedEvidence.ToArray()
    removed=@(); absentTargets=$missing.ToArray(); removedBytes=[long]0; freeBytesBefore=$freeBefore; preserved=$preservedRoots
}
function Save-CleanupReport { [IO.File]::WriteAllText($reportPath, ($record | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false)) }
Save-CleanupReport
try {
    foreach ($linkPath in $linksFound) {
        Assert-AllowedJunction $linkPath
        [IO.Directory]::Delete($linkPath) # Remove only this junction; never traverse canonical dependencies.
        $record.unlinkedJunctions += $linkPath
    }
    if (-not (Test-Path -LiteralPath $canonicalModules -PathType Container)) { throw 'Canonical dependencies disappeared after unlink.' }
    foreach ($target in $targets) {
        $checked = Assert-WorkspacePath $target.path
        # Recheck after unlink so Remove-Item cannot follow any newly introduced link.
        $null = Measure-CheckedTree $checked $false
        Remove-Item -LiteralPath $checked -Recurse -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $checked) { throw "Cleanup target still exists: $checked" }
        $removed.Add($target)
        $record.removed = $removed.ToArray()
        $record.removedBytes = [long](($removed | Measure-Object bytes -Sum).Sum)
        Save-CleanupReport
    }
    foreach ($relative in $preservedRoots) {
        if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $relative) -PathType Container)) { throw "Preserved directory disappeared: $relative" }
    }
    $record.result = 'PASS'
    $record.completedAtUtc = [DateTime]::UtcNow.ToString('o')
    $record.freeBytesAfter = [IO.DriveInfo]::new('C:\').AvailableFreeSpace
    $record.note = 'removedBytes counts logical generated-file sizes; free-space change can include concurrent filesystem activity.'
    Save-CleanupReport
} catch {
    $record.result = 'FAIL'
    $record.failedAtUtc = [DateTime]::UtcNow.ToString('o')
    $record.error = $_.Exception.Message
    $record.freeBytesAfter = [IO.DriveInfo]::new('C:\').AvailableFreeSpace
    Save-CleanupReport
    throw
}
[pscustomobject]@{ result=$record.result; removedBytes=$record.removedBytes; removedTargets=$removed.Count; copiedEvidence=$backups.Count; freeBytesAfter=$record.freeBytesAfter; report=$reportPath } | ConvertTo-Json -Depth 3
