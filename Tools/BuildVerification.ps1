param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor', [switch]$Release, [switch]$DisableBurst)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$verificationRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Temp/UnityBuildVerification'))
if (-not $verificationRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar)) { throw 'Verification path must remain inside workspace.' }
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    $dest = Join-Path $verificationRoot $folder
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Copy-Item -Path (Join-Path $projectRoot ($folder + '/*')) -Destination $dest -Recurse -Force
}
$logPath = Join-Path $projectRoot 'Temp/LumiaBuild.log'
$unityArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $verificationRoot + '"'), '-executeMethod', 'Lumia.Editor.LumiaProjectSetup.BuildWindows', '-logFile', ('"' + $logPath + '"'))
if (-not $Release) { $unityArgs += '-lumia-development' }
if ($DisableBurst) { $unityArgs += '--burst-disable-compilation' }
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
Write-Output ('Build process: ' + $process.Id)
Write-Output ('Build log: ' + $logPath)
