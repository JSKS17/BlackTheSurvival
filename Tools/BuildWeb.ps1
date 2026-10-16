param(
    [string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor',
    [ValidateSet('Gzip', 'Brotli', 'Disabled')][string]$Compression = 'Gzip',
    [string]$OutputDirectory = 'Build/Web',
    [switch]$Development,
    [switch]$LowMemory,
    [switch]$PrepareOnly,
    [switch]$Wait
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$buildProject = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Build/UnityWebBuild'))
$outputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { [IO.Path]::GetFullPath($OutputDirectory) } else { [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory)) }
$workspacePrefix = $projectRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $buildProject.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase) -or
    -not $outputRoot.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Build project and output must stay inside this workspace.'
}
$unityExecutable = Join-Path $UnityEditor 'Unity.exe'
if (-not (Test-Path -LiteralPath $unityExecutable -PathType Leaf)) { throw "Unity editor not found: $unityExecutable" }
if (-not $PrepareOnly) {
    $supportRoot = Join-Path $UnityEditor 'Data/PlaybackEngines/WebGLSupport'
    if (-not (Test-Path -LiteralPath $supportRoot -PathType Container)) {
        throw 'Web Build Support is missing. Install it for this editor version using Unity Hub > Installs > Add modules.'
    }
    $variationRoot = Join-Path $supportRoot 'Variations'
    $playerCore = if (Test-Path -LiteralPath $variationRoot -PathType Container) {
        Get-ChildItem -LiteralPath $variationRoot -Recurse -File -Filter 'UnityEngine.CoreModule.dll' | Select-Object -First 1
    }
    if (-not $playerCore) {
        throw 'Web Build Support is incomplete: player Variations/Managed assemblies are missing. Repair this editor version''s Web Build Support installation before building.'
    }
}
New-Item -ItemType Directory -Path $buildProject -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    $source = Join-Path $projectRoot $folder
    $destination = [IO.Path]::GetFullPath((Join-Path $buildProject $folder))
    # /MIR removes stale snapshot files only in these validated, isolated project folders.
    if (-not $destination.StartsWith($buildProject + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid snapshot destination.' }
    & robocopy $source $destination /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -gt 7) { throw "Snapshot failed for $folder (robocopy exit $LASTEXITCODE)." }
}
if ($PrepareOnly) {
    [pscustomobject]@{ Project = $buildProject; Output = $outputRoot; Prepared = $true }
    return
}
$logPath = Join-Path $projectRoot 'Build/LumiaWebBuild.log'
# Select WebGL before the initial import, avoiding a full Windows asset import first.
$unityArgs = @('-batchmode', '-nographics', '-buildTarget', 'WebGL', '-projectPath', ('"' + $buildProject + '"'),
    '-executeMethod', 'Lumia.Editor.LumiaWebBuild.Build', '-logFile', ('"' + $logPath + '"'),
    '-lumia-web-output', ('"' + $outputRoot + '"'), '-lumia-web-compression', $Compression)
if ($Development) { $unityArgs += '-lumia-development' }
if ($LowMemory) { $unityArgs += @('-job-worker-count', '1', '-import-worker-count', '1') }
$process = Start-Process -FilePath $unityExecutable -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
[pscustomobject]@{ ProcessId = $process.Id; Log = $logPath; Project = $buildProject; Output = $outputRoot }
if ($Wait) {
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity web build failed with exit code $($process.ExitCode). See $logPath" }
    $manifestPath = Join-Path $outputRoot 'build-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Unity exited without a web build manifest. See $logPath" }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.result -ne 'Succeeded') { throw "Unity web build result: $($manifest.result). See $logPath" }
    $manifest
}
