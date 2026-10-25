#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Inspect','Publish')][string] $Mode = 'Inspect',
    [string] $Repository = 'JSKS17/BlackTheSurvival',
    [string] $Version = '1.0.27',
    [Parameter(Mandatory)][string] $SourceCommit,
    [Parameter(Mandatory)][string] $ZipPath,
    [Parameter(Mandatory)][string] $ChecksumsPath,
    [Parameter(Mandatory)][string] $NotesPath,
    [Parameter(Mandatory)][string] $WindowsManifestPath,
    [string] $SourceFreezePath = (Join-Path $PSScriptRoot 'source-validation.json'),
    [Security.SecureString] $Token,
    [string] $OutputProofPath = (Join-Path $PSScriptRoot 'github-publish-state.json')
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..')).TrimEnd('\')
$comparison = [StringComparison]::OrdinalIgnoreCase
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$' -or $Version -notmatch '^\d+\.\d+\.\d+$' -or $SourceCommit -notmatch '^[a-f0-9]{40}$') { throw 'Invalid repository, version or exact source commit.' }
function Local-File([string] $value) {
    $absolute=[IO.Path]::GetFullPath($value)
    if (-not $absolute.StartsWith($workspace+'\',$comparison)) { throw 'Release input must remain in this workspace.' }
    $cursor=$absolute
    while (-not $cursor.Equals($workspace,$comparison)) {
        if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Linked release input refused.' }
        $cursor=[IO.Path]::GetDirectoryName($cursor)
    }
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw 'Missing release input.' }
    return $absolute
}
$ZipPath=Local-File $ZipPath; $ChecksumsPath=Local-File $ChecksumsPath; $NotesPath=Local-File $NotesPath
$WindowsManifestPath=Local-File $WindowsManifestPath; $SourceFreezePath=Local-File $SourceFreezePath
$OutputProofPath=[IO.Path]::GetFullPath($OutputProofPath)
if (-not ([IO.Path]::GetDirectoryName($OutputProofPath)).Equals($PSScriptRoot,$comparison)) { throw 'Public metadata proof must be in this helper directory.' }
$tag='v'+$Version
$zipName='BlackTheSurvival-'+$tag+'-Windows-x64.zip'; $sumsName='SHA256SUMS-'+$tag+'.txt'
if ([IO.Path]::GetFileName($ZipPath) -cne $zipName -or [IO.Path]::GetFileName($ChecksumsPath) -cne $sumsName) { throw 'Release asset filenames must follow the repository pattern.' }
$freeze=Get-Content -LiteralPath $SourceFreezePath -Raw | ConvertFrom-Json
$windows=Get-Content -LiteralPath $WindowsManifestPath -Raw | ConvertFrom-Json
$freezeSha=(Get-FileHash -LiteralPath $SourceFreezePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($freeze.result -ne 'PASS' -or $freeze.version -ne $Version -or $windows.result -ne 'Succeeded' -or $windows.productVersion -ne $Version -or $windows.errors -ne 0 -or $windows.development -ne $false) { throw 'Matching frozen source and successful production Windows build proofs are required.' }
if ($windows.sourceCaptureSha256 -ne $freezeSha) { throw 'Windows build must capture the exact final source-validation.json hash.' }
$zipSha=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sumText=Get-Content -LiteralPath $ChecksumsPath -Raw
if ($sumText.Length -gt 4096 -or $sumText.Trim() -cne ($zipSha+'  '+$zipName)) { throw 'Checksum asset must contain the exact ZIP SHA256 and filename.' }
$notes=Get-Content -LiteralPath $NotesPath -Raw
if ([string]::IsNullOrWhiteSpace($notes) -or $notes.Length -gt 524288) { throw 'Release notes must be nonempty and bounded.' }
$archive=[IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $entryNames=@($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    if ('BlackTheSurvival.exe' -notin $entryNames -or 'UnityPlayer.dll' -notin $entryNames -or -not ($entryNames | Where-Object { $_.StartsWith('BlackTheSurvival_Data/') })) { throw 'ZIP must contain the complete Windows player at its root.' }
} finally { $archive.Dispose() }
$assets=@(
    @{path=$ZipPath;name=$zipName;sha256=$zipSha;size=(Get-Item -LiteralPath $ZipPath).Length;contentType='application/zip'},
    @{path=$ChecksumsPath;name=$sumsName;sha256=(Get-FileHash -LiteralPath $ChecksumsPath -Algorithm SHA256).Hash.ToLowerInvariant();size=(Get-Item -LiteralPath $ChecksumsPath).Length;contentType='text/plain'}
)
$previousPrompt=$env:GIT_TERMINAL_PROMPT
$credentialLines=$null; $password=$null; $script:ApiToken=$Token
try {
    if ($null -eq $script:ApiToken) {
        $env:GIT_TERMINAL_PROMPT='0'
        $credentialLines=@("protocol=https","host=github.com","") | & git -c credential.interactive=never credential fill 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Git credential manager could not provide GitHub authentication.' }
        $password=($credentialLines | Where-Object { $_.StartsWith('password=') } | Select-Object -First 1)
        if (-not $password) { throw 'GitHub credential was unavailable.' }
        $script:ApiToken=ConvertTo-SecureString -String $password.Substring(9) -AsPlainText -Force
        $credentialLines=$null; $password=$null
    }
    function Api([string] $Method,[string] $Uri,[object] $Body=$null,[string] $InputFile=$null,[string] $ContentType='application/json',[switch] $Allow404) {
        $target=[Uri]$Uri
        if ($target.Scheme -ne 'https' -or $target.Host -notin @('api.github.com','uploads.github.com')) { throw 'GitHub authentication destination refused.' }
        $arguments=@{Method=$Method;Uri=$Uri;Authentication='Bearer';Token=$script:ApiToken;Headers=@{Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='BlackTheSurvival-release-helper'};ErrorAction='Stop'}
        if ($InputFile) { $arguments.InFile=$InputFile; $arguments.ContentType=$ContentType }
        elseif ($null -ne $Body) { $arguments.Body=($Body | ConvertTo-Json -Depth 8 -Compress);$arguments.ContentType='application/json; charset=utf-8' }
        try { return Invoke-RestMethod @arguments }
        catch {
            $status=0; if ($_.Exception.Response) { $status=[int]$_.Exception.Response.StatusCode }
            if ($Allow404 -and $status -eq 404) { return $null }
            throw ('GitHub request failed (HTTP '+$status+'); credentials and response body were not logged.')
        }
    }
    $api='https://api.github.com/repos/'+$Repository
    $tagRecord=Api GET ($api+'/git/ref/tags/'+[Uri]::EscapeDataString($tag))
    $target=$tagRecord.object; $depth=0
    while ($target.type -eq 'tag' -and $depth++ -lt 4) { $target=(Api GET ($api+'/git/tags/'+$target.sha)).object }
    if ($target.type -ne 'commit' -or $target.sha -ne $SourceCommit) { throw 'Remote tag must already resolve to the exact approved source commit.' }
    $release=Api GET ($api+'/releases/tags/'+[Uri]::EscapeDataString($tag)) -Allow404
    if ($Mode -eq 'Inspect') {
        [pscustomobject]@{result='READY';mode='Inspect';repository=$Repository;tag=$tag;sourceCommit=$SourceCommit;existingReleaseId=$release.id;assets=@($assets | ForEach-Object {[pscustomobject]@{name=$_.name;bytes=$_.size;sha256=$_.sha256}});sourceFreezeSha256=$freezeSha;writesPerformed=$false} | ConvertTo-Json -Depth 6
        return
    }
    if (-not $release) { $release=Api POST ($api+'/releases') @{tag_name=$tag;target_commitish=$SourceCommit;name=('Black The Survival '+$tag);body=$notes;draft=$true;prerelease=$false} }
    if ($release.tag_name -ne $tag -or $release.prerelease) { throw 'Existing release identity is incompatible.' }
    foreach ($asset in $assets) {
        $existing=@($release.assets | Where-Object { $_.name -ceq $asset.name })
        if ($existing.Count -gt 1) { throw 'Duplicate remote release asset.' }
        if ($existing.Count -eq 0) {
            if (-not $release.draft) { throw 'Published release is missing an asset; refusing to alter it.' }
            $upload=$release.upload_url.Split('{')[0]+'?name='+[Uri]::EscapeDataString($asset.name)
            $remoteAsset=Api POST $upload -InputFile $asset.path -ContentType $asset.contentType
        } else { $remoteAsset=$existing[0] }
        if ($remoteAsset.size -ne $asset.size -or $remoteAsset.digest -ne ('sha256:'+$asset.sha256) -or $remoteAsset.state -ne 'uploaded') { throw 'Remote asset size/digest/state mismatch; no asset was deleted or replaced.' }
    }
    if ($release.draft) { $release=Api PATCH ($api+'/releases/'+$release.id) @{draft=$false} }
    $release=Api GET ($api+'/releases/'+$release.id)
    if ($release.draft -or $release.prerelease -or -not $release.published_at) { throw 'Final release is not a published production release.' }
    $remoteProof=@()
    foreach ($asset in $assets) {
        $remote=@($release.assets | Where-Object { $_.name -ceq $asset.name })
        if ($remote.Count -ne 1 -or $remote[0].size -ne $asset.size -or $remote[0].digest -ne ('sha256:'+$asset.sha256)) { throw 'Published asset verification failed.' }
        $remoteProof += [ordered]@{name=$asset.name;bytes=$asset.size;sha256=$asset.sha256;id=$remote[0].id;url=$remote[0].browser_download_url}
    }
    $record=[ordered]@{result='PASS';completedAtUtc=[DateTime]::UtcNow.ToString('o');version=$Version;repository=$Repository;tag=$tag;sourceCommit=$SourceCommit;sourceFreezeSha256=$freezeSha;windowsManifestSha256=(Get-FileHash -LiteralPath $WindowsManifestPath -Algorithm SHA256).Hash.ToLowerInvariant();releaseId=$release.id;url=$release.html_url;publishedAt=$release.published_at;assets=$remoteProof;credentialsLogged=$false;gitStateModified=$false}
    [IO.File]::WriteAllText($OutputProofPath,($record | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    [pscustomobject]@{result='PASS';url=$release.html_url;tag=$tag;assets=$remoteProof.Count;proof=$OutputProofPath} | ConvertTo-Json
} finally {
    $script:ApiToken=$null; $credentialLines=$null; $password=$null; $Token=$null
    if ($null -eq $previousPrompt) { Remove-Item Env:GIT_TERMINAL_PROMPT -ErrorAction SilentlyContinue } else { $env:GIT_TERMINAL_PROMPT=$previousPrompt }
}
