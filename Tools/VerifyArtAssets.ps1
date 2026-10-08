param(
    [string]$Python,
    [switch]$AllowIncomplete
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $Python) {
    $bundledPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
    if (Test-Path -LiteralPath $bundledPython) { $Python = $bundledPython }
    else { $Python = (Get-Command python -ErrorAction Stop).Source }
}
$scriptArgs = @((Join-Path $PSScriptRoot 'VerifyArtAssets.py'), '--root', $projectRoot)
if ($AllowIncomplete) { $scriptArgs += '--allow-incomplete' }
& $Python @scriptArgs
if ($LASTEXITCODE -ne 0) { throw 'Generated game art verification failed. See docs/art-chibi/art-validation.json.' }
