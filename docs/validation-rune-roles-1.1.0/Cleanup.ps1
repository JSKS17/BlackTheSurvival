#requires -Version 7.0
[CmdletBinding()]
param([switch] $VerifiedServersStopped)

$ErrorActionPreference = 'Stop'
if (-not $VerifiedServersStopped) { throw 'Stop this task''s Unity, build, test and preview processes before passing -VerifiedServersStopped.' }
$taskRoot = (Resolve-Path -LiteralPath 'C:\Users\azxar\BlackTheSurvival').ProviderPath.TrimEnd('\')
$proofRoot = Join-Path $taskRoot 'docs\validation-rune-roles-1.1.0'
$stage = Join-Path $taskRoot 'Temp\SiteRuneRoles110'
$release = Join-Path $taskRoot 'Temp\RuneRolesRelease110'
$build = Join-Path $taskRoot 'Build'
$canonicalModules = Join-Path $taskRoot 'WebSite\node_modules'
$ordinal = [StringComparison]::OrdinalIgnoreCase

function Checked-Path([string] $path) {
    $absolute = [IO.Path]::GetFullPath($path).TrimEnd('\')
    if (-not $absolute.StartsWith($taskRoot + '\', $ordinal)) { throw "Path is outside this workspace: $absolute" }
    $parent = [IO.Path]::GetDirectoryName($absolute)
    while ($parent -and -not $parent.Equals($taskRoot, $ordinal)) {
        if (-not $parent.StartsWith($taskRoot + '\', $ordinal)) { throw 'Unexpected parent outside workspace.' }
        if ([IO.Directory]::Exists($parent) -and ([IO.File]::GetAttributes($parent) -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked parent is not allowed: $parent" }
        $parent = [IO.Path]::GetDirectoryName($parent)
    }
    return $absolute
}

function Read-Proof([string] $name) {
    $file = Checked-Path (Join-Path $proofRoot $name)
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing required proof: $name" }
    if ([IO.File]::GetAttributes($file) -band [IO.FileAttributes]::ReparsePoint) { throw 'Proof must be an ordinary file.' }
    return Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
}

$publish = Read-Proof 'publish-state.json'
if ($publish.deployment.status -ne 'succeeded') { throw 'Deployment must succeed before cleanup.' }
if ($publish.saved.version_number -le 57) { throw 'This deployment must follow baseline Site version 57.' }
if (-not [IO.Path]::GetFullPath($publish.package.checkout_path).Equals($stage, $ordinal)) { throw 'Publication checkout does not belong to this release.' }
if ($publish.saved.id -ne $publish.deployment.version_id -or $publish.package.commit_sha -ne $publish.saved.source.commit_sha) { throw 'Publication identities must agree.' }
foreach ($name in @('source-validation.json','live-validation.json','browser-validation.json')) {
    $proof = Read-Proof $name
    if ($proof.result -ne 'PASS') { throw "Required verification has not passed: $name" }
}
$sync = Read-Proof 'release-sync.json'
if ($sync.result -ne 'PASS') { throw 'Canonical source sync must pass before deleting the owned release package.' }
$presenceFrozen = Read-Proof 'source-validation.json'
$testFreezeSha = (Get-FileHash -LiteralPath (Checked-Path (Join-Path $proofRoot 'source-validation.json')) -Algorithm SHA256).Hash.ToLowerInvariant()
$executedTests = Read-Proof 'gameplay-tests.json'
if (($executedTests.result -ne 'PASS' -and $executedTests.status -ne 'PASS') -or $executedTests.sourceFreezeSha256 -ne $testFreezeSha) { throw 'Successful tests must bind to the unchanged source freeze.' }
$testAmendment = $null
$testAmendmentRecord = $null
$testAmendedSources = [ordered]@{}
$testAllowedSources = @('Tools/CoreSmokeTests.cs','Tools/SharedPassiveResourceSmokeTests.cs')
$testAmendmentPath = Checked-Path (Join-Path $proofRoot 'test-source-amendment.json')
if (Test-Path -LiteralPath $testAmendmentPath) {
    $testAmendment = Read-Proof 'test-source-amendment.json'
    if ($testAmendment.result -ne 'PASS' -or $testAmendment.sourceFreezeSha256 -ne $testFreezeSha -or $testAmendment.productSourceChanged -ne $false -or $testAmendment.approvedByRoot -ne $true) { throw 'A source amendment requires an approved test-only proof bound to the original freeze.' }
    if (@($testAmendment.changes).Count -lt 1 -or @($testAmendment.changes).Count -gt $testAllowedSources.Count) { throw 'Only the approved one- or two-test source subset may be amended.' }
    $testAmendmentHash = (Get-FileHash -LiteralPath $testAmendmentPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($executedTests.testSourceAmendmentSha256 -cne $testAmendmentHash) { throw 'Executed tests do not bind to the exact amendment proof bytes.' }
    $testAmendmentChanges = [Collections.Generic.List[object]]::new()
    foreach ($testChange in @($testAmendment.changes)) {
        if ($testAllowedSources -cnotcontains $testChange.path -or $testAmendedSources.Contains($testChange.path)) { throw ('Unapproved or duplicate amended test source: ' + $testChange.path) }
        $testOriginalHash = $presenceFrozen.tools.PSObject.Properties[$testChange.path].Value
        if ($testChange.frozenSha256 -cne $testOriginalHash -or $testChange.amendedSha256 -cnotmatch '^[a-f0-9]{64}$' -or $testChange.amendedSha256 -ceq $testOriginalHash) { throw 'Amendment hashes must identify the original frozen test and its different approved execution source.' }
        $testCurrentHash = (Get-FileHash -LiteralPath (Checked-Path (Join-Path $taskRoot $testChange.path)) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($testCurrentHash -cne $testChange.amendedSha256 -or $executedTests.testSourceHashes.PSObject.Properties[$testChange.path].Value -cne $testCurrentHash -or $executedTests.sourceHashes.PSObject.Properties[$testChange.path].Value -cne $testCurrentHash) { throw 'Approved amended test must equal the current source and both executed-source maps.' }
        $testPreserved = [string]$testChange.preservedOriginalTest
        if (-not $testPreserved -and $testChange.path -ceq 'Tools/CoreSmokeTests.cs') { $testPreserved = [string]$testAmendment.preservedOriginalTest }
        if (-not $testPreserved -or [IO.Path]::IsPathRooted($testPreserved) -or $testPreserved.Contains('..')) { throw 'Preserved original test must be a proof-relative ordinary file.' }
        $testPreservedHash = (Get-FileHash -LiteralPath (Checked-Path (Join-Path $proofRoot $testPreserved)) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($testPreservedHash -cne $testOriginalHash) { throw 'Preserved original test must match the immutable freeze.' }
        $testAmendedSources[$testChange.path] = $testCurrentHash
        $testAmendmentChanges.Add([ordered]@{path=$testChange.path;frozenSha256=$testOriginalHash;amendedSha256=$testCurrentHash;preservedOriginalTest=$testPreserved})
    }
    $testAmendmentRecord = [ordered]@{amendmentSha256=$testAmendmentHash;changes=$testAmendmentChanges.ToArray()}
} elseif ($executedTests.testSourceAmendmentSha256) { throw 'Test proof refers to a missing amendment.' }
foreach ($presenceName in @('product','unchangedArt','tools','sourceDocumentation')) {
    foreach ($presenceField in $presenceFrozen.$presenceName.PSObject.Properties) {
        $presenceSource = Checked-Path (Join-Path $taskRoot $presenceField.Name)
        $presenceExpected = if ($presenceName -ceq 'tools' -and $testAmendedSources.Contains($presenceField.Name)) { $testAmendedSources[$presenceField.Name] } else { $presenceField.Value }
        if ((Get-FileHash -LiteralPath $presenceSource -Algorithm SHA256).Hash.ToLowerInvariant() -cne $presenceExpected) { throw ('Frozen source changed before cleanup: ' + $presenceField.Name) }
    }
}
foreach ($executedTest in $executedTests.testSourceHashes.PSObject.Properties) {
    $executedExpected = $presenceFrozen.tools.PSObject.Properties[$executedTest.Name].Value
    if (-not $executedExpected) { throw ('Unfrozen executed test source: ' + $executedTest.Name) }
    if ($testAmendedSources.Contains($executedTest.Name)) { $executedExpected = $testAmendedSources[$executedTest.Name] }
    if ($executedTest.Value -cne $executedExpected) { throw ('Executed test source does not match its allowed freeze: ' + $executedTest.Name) }
}
foreach ($presenceProofName in @('gameplay-tests.json','unity-build-observer-result.json')) {
    $presenceVerified = Read-Proof $presenceProofName
    if ($presenceVerified.result -ne 'PASS' -and $presenceVerified.status -ne 'PASS') { throw ('Required check failed: ' + $presenceProofName) }
}
$presenceServerSync = Read-Proof 'web-source-sync.json'
if ($presenceServerSync.result -ne 'PASS') { throw 'Current web source sync required.' }
foreach ($presenceField in $presenceServerSync.files.PSObject.Properties) {
    $presenceSource = Checked-Path (Join-Path (Join-Path $taskRoot 'WebSite') $presenceField.Name)
    if ((Get-FileHash -LiteralPath $presenceSource -Algorithm SHA256).Hash.ToLowerInvariant() -ne $presenceField.Value) { throw ('Published server source changed: ' + $presenceField.Name) }
}
$canonicalManifest = Checked-Path (Join-Path $taskRoot 'WebSite\lib\unity-assets-manifest.json')
if ((Get-FileHash -LiteralPath $canonicalManifest -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sync.installed.manifestSha256) { throw 'Canonical game manifest no longer matches the validated source sync.' }
$live = Read-Proof 'live-validation.json'
if ($live.gameVersion -ne '1.1.0' -or $live.publication.versionId -ne $publish.saved.id -or $live.publication.commitSha -ne $publish.package.commit_sha) { throw 'Live verification does not match this publication/version.' }
$attributes = Checked-Path (Join-Path $taskRoot 'WebSite\.gitattributes')
$expectedAttributes = 'fcd4902b9b599e41443f819411a5c47fb77218e5efd2b7be19b62841675ccc5a'
if ((Get-FileHash -LiteralPath $attributes -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedAttributes) { throw 'Protected .gitattributes changed.' }

$preserved = @('Assets','ArtSource','Packages','ProjectSettings','Tools','docs','WebSite\build','WebSite\public\game\Build','WebSite\.sites-runtime','WebSite\node_modules')
foreach ($relative in $preserved) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $relative) -PathType Container)) { throw "Missing preserved source/dependency directory: $relative" }
}

# Read-only process check. This helper never stops or kills any process.
# Requires an elevated read on hosts where ordinary CIM access is unavailable.
$processes = Get-CimInstance Win32_Process -ErrorAction Stop
$previewAbsolute = Join-Path $proofRoot 'Preview.mjs'
$previewRelative = [IO.Path]::GetRelativePath($taskRoot, $previewAbsolute)
$previewPaths = @($previewAbsolute, $previewAbsolute.Replace('\','/'), $previewRelative, $previewRelative.Replace('\','/'))
$blocked = @($processes | Where-Object {
    if ($_.ProcessId -eq $PID -or $_.Name -in @('pwsh.exe','powershell.exe','conhost.exe','cmd.exe')) { return $false }
    $cmd = [string]$_.CommandLine
    $cmd.IndexOf($stage, $ordinal) -ge 0 -or $cmd.IndexOf($stage.Replace('\','/'), $ordinal) -ge 0 -or
    $cmd.IndexOf($release, $ordinal) -ge 0 -or $cmd.IndexOf($release.Replace('\','/'), $ordinal) -ge 0 -or
    $cmd.IndexOf($build + '\', $ordinal) -ge 0 -or $cmd.IndexOf(($build + '\').Replace('\','/'), $ordinal) -ge 0 -or
    ($previewPaths | Where-Object { $cmd.IndexOf($_, $ordinal) -ge 0 })
})
if ($blocked.Count) { throw ('Task processes still run; refusing cleanup. PIDs: ' + (($blocked | ForEach-Object { $_.ProcessId }) -join ', ')) }

# The whole root Build tree is owned by this release only if its current direct
# children still match this exact whitelist. A new task's output aborts cleanup.
$build = Checked-Path $build
if (Test-Path -LiteralPath $build) {
    $rootItem = Get-Item -LiteralPath $build -Force
    if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Build root must not be linked.' }
    $rawOutput = Checked-Path (Join-Path $build 'Web110')
    if (Test-Path -LiteralPath $rawOutput) {
        $manifest = Read-Proof 'unity-build-manifest.json'
        if ($manifest.result -ne 'Succeeded' -or $manifest.productVersion -ne '1.1.0' -or $manifest.errors -ne 0) { throw 'Raw build ownership proof must match this release.' }
        $actual = Checked-Path (Join-Path $rawOutput 'build-manifest.json')
        $saved = Checked-Path (Join-Path $proofRoot 'unity-build-manifest.json')
        if ((Get-FileHash -LiteralPath $actual -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash) { throw 'Raw output no longer matches the saved release build proof.' }
    }
    # Extend only after inspecting a named task test output and its proof.
    $allowed = @('UnityWebBuild','Web110','LumiaWebBuild.log')
    $unexpected = @(Get-ChildItem -LiteralPath $build -Force | Where-Object { $_.Name -notin $allowed })
    if ($unexpected.Count) { throw ('Unrecognized Build entries require inspection: ' + (($unexpected | ForEach-Object { $_.Name }) -join ', ')) }
}

$junction = Checked-Path (Join-Path $stage 'node_modules')
$junctionFound = $false
function Assert-OwnedJunction([string] $path) {
    $absolute = Checked-Path $path
    if (-not $absolute.Equals($junction, $ordinal)) { throw "Unexpected link inside cleanup target: $absolute" }
    $item = Get-Item -LiteralPath $absolute -Force
    $targets = @($item.Target)
    if ($item.LinkType -ne 'Junction' -or $targets.Count -ne 1 -or -not ([IO.Path]::GetFullPath([string]$targets[0]).TrimEnd('\')).Equals($canonicalModules, $ordinal)) { throw 'Unexpected dependency junction target.' }
    $script:junctionFound = $true
}

function Measure-Checked([string] $path, [bool] $permitJunction = $true) {
    $absolute = Checked-Path $path
    $item = Get-Item -LiteralPath $absolute -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Cleanup root must be ordinary: $absolute" }
    if (-not $item.PSIsContainer) { return [pscustomobject]@{bytes=[long]$item.Length;files=[long]1} }
    $stack = [Collections.Generic.Stack[string]]::new(); $stack.Push($absolute)
    $bytes = [long]0; $files = [long]0
    while ($stack.Count -gt 0) {
        foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($stack.Pop())) {
            $attributes = [IO.File]::GetAttributes($entry)
            if ($attributes -band [IO.FileAttributes]::ReparsePoint) {
                if (-not $permitJunction) { throw "A link remains after preflight/unlink: $entry" }
                Assert-OwnedJunction $entry; continue
            }
            if ($attributes -band [IO.FileAttributes]::Directory) { $stack.Push($entry) }
            else { $bytes += [IO.FileInfo]::new($entry).Length; $files++ }
        }
    }
    return [pscustomobject]@{bytes=$bytes;files=$files}
}

$specs = @(
    @{relative='Build';kind='Disposable Unity build project, raw Web110 export and release build log'},
    @{relative='Temp\SiteRuneRoles110';kind='Completed official publication checkout and local runtime files'},
    @{relative='Temp\RuneRolesRelease110';kind='Owned release package, isolated uploader and completed swap transaction backups'},
    @{relative='docs\validation-rune-roles-1.1.0\site.tar.gz';kind='Successfully saved deployment archive'},
    @{relative='docs\validation-rune-roles-1.1.0\bridge.tar.gz';kind='Completed old-game upload transition archive'}
)
$targets = [Collections.Generic.List[object]]::new()
$absent = [Collections.Generic.List[string]]::new()
foreach ($spec in $specs) {
    $absolute = Checked-Path (Join-Path $taskRoot $spec.relative)
    if (-not (Test-Path -LiteralPath $absolute)) { $absent.Add($absolute); continue }
    $size = Measure-Checked $absolute
    $targets.Add([pscustomobject]@{path=$absolute;bytes=$size.bytes;files=$size.files;kind=$spec.kind})
}

$record = [ordered]@{
    result='IN_PROGRESS'; startedAtUtc=[DateTime]::UtcNow.ToString('o'); workspace=$taskRoot
    gates=@('Deployment succeeded','Publication identity matches','Source PASS','Live PASS','Browser PASS','Canonical release sync PASS','Verified servers stopped','CIM process inspection')
    targets=$targets.ToArray(); absentTargets=$absent.ToArray(); removed=@(); removedBytes=[long]0
    freeBytesBefore=[IO.DriveInfo]::new('C:\').AvailableFreeSpace
    unlinkedJunctions=@(); preserved=$preserved; noProcessWasStopped=$true
    testSourceAmendment=$testAmendmentRecord
}
$reportFile = Join-Path $proofRoot 'cleanup.json'
function Save-Report { [IO.File]::WriteAllText($reportFile,($record | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false)) }
Save-Report
try {
    if ($junctionFound) {
        Assert-OwnedJunction $junction
        [IO.Directory]::Delete($junction) # Unlink this junction only; never traverse canonical node_modules.
        $record.unlinkedJunctions += $junction
    }
    if (-not (Test-Path -LiteralPath $canonicalModules -PathType Container)) { throw 'Canonical dependencies disappeared after unlink.' }
    $removed = [Collections.Generic.List[object]]::new()
    foreach ($target in $targets) {
        $absolute = Checked-Path $target.path
        $null = Measure-Checked $absolute $false
        Remove-Item -LiteralPath $absolute -Recurse -Force -ErrorAction Stop
        if (Test-Path -LiteralPath $absolute) { throw "Cleanup target remains: $absolute" }
        $removed.Add($target); $record.removed=$removed.ToArray()
        $record.removedBytes=[long](($removed | Measure-Object bytes -Sum).Sum)
        Save-Report
    }
    foreach ($relative in $preserved) {
        if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $relative) -PathType Container)) { throw "Preserved source disappeared: $relative" }
    }
    if ((Get-FileHash -LiteralPath $attributes -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedAttributes) { throw 'Protected .gitattributes changed during cleanup.' }
    $record.result='PASS'; $record.completedAtUtc=[DateTime]::UtcNow.ToString('o')
    $record.freeBytesAfter=[IO.DriveInfo]::new('C:\').AvailableFreeSpace
    $record.note='Removed bytes are logical generated-file sizes; concurrent filesystem activity may change the free-space delta.'
    Save-Report
} catch {
    $record.result='FAIL'; $record.error=$_.Exception.Message
    $record.failedAtUtc=[DateTime]::UtcNow.ToString('o')
    $record.freeBytesAfter=[IO.DriveInfo]::new('C:\').AvailableFreeSpace
    Save-Report; throw
}
[pscustomobject]@{result=$record.result;removedBytes=$record.removedBytes;removedTargets=$record.removed.Count;freeBytesAfter=$record.freeBytesAfter;report=$reportFile} | ConvertTo-Json
