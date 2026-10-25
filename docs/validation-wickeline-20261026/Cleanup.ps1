#requires -Version 7.0
[CmdletBinding()]
param([switch] $VerifiedServersStopped)

$ErrorActionPreference = 'Stop'
if (-not $VerifiedServersStopped) { throw 'Stop this task''s Unity, build, test and preview processes before passing -VerifiedServersStopped.' }
$taskRoot = (Resolve-Path -LiteralPath 'C:\Users\azxar\BlackTheSurvival').ProviderPath.TrimEnd('\')
$proofRoot = Join-Path $taskRoot 'docs\validation-wickeline-20261026'
$stage = Join-Path $taskRoot 'Temp\SiteWickeline20261026'
$release = Join-Path $taskRoot 'Temp\WickelineRelease20261026'
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
if ($publish.saved.version_number -le 53) { throw 'This deployment must follow baseline Site version 53.' }
if (-not [IO.Path]::GetFullPath($publish.package.checkout_path).Equals($stage, $ordinal)) { throw 'Publication checkout does not belong to this release.' }
if ($publish.saved.id -ne $publish.deployment.version_id -or $publish.package.commit_sha -ne $publish.saved.source.commit_sha) { throw 'Publication identities must agree.' }
foreach ($name in @('source-validation.json','live-validation.json','browser-validation.json')) {
    $proof = Read-Proof $name
    if ($proof.result -ne 'PASS') { throw "Required verification has not passed: $name" }
}
$sync = Read-Proof 'release-sync.json'
if ($sync.result -ne 'PASS') { throw 'Canonical source sync must pass before deleting the owned release package.' }
$github = Read-Proof 'github-publish-state.json'
if ($github.result -ne 'PASS' -or $github.version -ne '1.0.27' -or $github.tag -ne 'v1.0.27' -or $github.assets.Count -ne 2) {throw 'Verified GitHub release and both Windows assets must exist before cleanup.'}
$windows = Read-Proof 'windows-build-manifest.json'
if ($windows.result -ne 'Succeeded' -or $windows.productVersion -ne '1.0.27' -or $windows.errors -ne 0) {throw 'Windows build proof required before cleanup.'}
$canonicalManifest = Checked-Path (Join-Path $taskRoot 'WebSite\lib\unity-assets-manifest.json')
if ((Get-FileHash -LiteralPath $canonicalManifest -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sync.installed.manifestSha256) { throw 'Canonical game manifest no longer matches the validated source sync.' }
$live = Read-Proof 'live-validation.json'
if ($live.gameVersion -ne '1.0.27' -or $live.publication.versionId -ne $publish.saved.id -or $live.publication.commitSha -ne $publish.package.commit_sha) { throw 'Live verification does not match this publication/version.' }
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
    $rawOutput = Checked-Path (Join-Path $build 'Web127')
    if (Test-Path -LiteralPath $rawOutput) {
        $manifest = Read-Proof 'unity-build-manifest.json'
        if ($manifest.result -ne 'Succeeded' -or $manifest.productVersion -ne '1.0.27' -or $manifest.errors -ne 0) { throw 'Raw build ownership proof must match this release.' }
        $actual = Checked-Path (Join-Path $rawOutput 'build-manifest.json')
        $saved = Checked-Path (Join-Path $proofRoot 'unity-build-manifest.json')
        if ((Get-FileHash -LiteralPath $actual -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash) { throw 'Raw output no longer matches the saved release build proof.' }
    }
    # Extend only after inspecting a named task test output and its proof.
    $allowed = @('UnityWebBuild','Web127','LumiaWebBuild.log')
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
    @{relative='Build';kind='Disposable Unity build project, raw Web127 export and release build log'},
    @{relative='Temp\SiteWickeline20261026';kind='Completed official publication checkout and local runtime files'},
    @{relative='Temp\WickelineRelease20261026';kind='Owned release package, isolated uploader and completed swap transaction backups'},
    @{relative='docs\validation-wickeline-20261026\site.tar.gz';kind='Successfully saved deployment archive'},
    @{relative='docs\validation-wickeline-20261026\bridge.tar.gz';kind='Completed old-game upload transition archive'}
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
