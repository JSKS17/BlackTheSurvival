param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$temporaryParent = Join-Path $workspace 'Temp/TouchScrollTests'
$temporary = [IO.Path]::GetFullPath((Join-Path $temporaryParent ([guid]::NewGuid().ToString('N'))))
if (-not $temporary.StartsWith(([IO.Path]::GetFullPath($temporaryParent) + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) { throw 'Test output must stay within Temp/TouchScrollTests.' }
[IO.Directory]::CreateDirectory($temporary) | Out-Null
$executable = Join-Path $temporary 'TouchScrollTests.exe'
$source = Join-Path $workspace 'Assets/Scripts/Lumia/TouchScrollGesture.cs'
$tests = Join-Path $PSScriptRoot 'TouchScrollSmokeTests.cs'
$evidence = Join-Path $workspace 'Build/SwipeScrollVerification/touch-scroll-core-tests.json'
$clock = [Diagnostics.Stopwatch]::StartNew()
$exitCode = 1
$output = @()
$report = $null
$temporaryFilesRemoved = $false
try {
    $monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
    $compiler = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
    $dotnet = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
    $arguments = @($compiler, '/nologo', '/target:exe', '/langversion:latest', ('/out:' + $executable), '/nostdlib+',
        ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), $source, $tests)
    & $dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Touch-scroll core tests did not compile.' }
    $output = @(& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executable 2>&1 | ForEach-Object { $_.ToString() })
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Output $_ }
    $marker = $output | ForEach-Object { $_.TrimStart([char]0xFEFF) } | Where-Object { $_.StartsWith('TOUCH_SCROLL_RESULT ') } | Select-Object -Last 1
    if (-not $marker) { throw 'Touch-scroll test executable returned no result marker.' }
    $result = $marker.Substring('TOUCH_SCROLL_RESULT '.Length) | ConvertFrom-Json
    if ($exitCode -ne 0 -or $result.result -ne 'PASS') { throw 'Touch-scroll behavioral regressions failed.' }
    $hashes = [ordered]@{}
    foreach ($filename in @($source, $tests, $PSCommandPath)) {
        $relative = $filename.Substring($workspace.Length + 1).Replace('\','/')
        $hashes[$relative] = (Get-FileHash -LiteralPath $filename -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $report = [ordered]@{result='PASS';checkedAtUtc=[datetime]::UtcNow.ToString('o');assertions=$result.assertions;scenarios=$result.scenarios;sourceHashes=$hashes}
    $exitCode = 0
} catch {
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
    $report = [ordered]@{result='FAIL';checkedAtUtc=[datetime]::UtcNow.ToString('o');error=$_.Exception.Message;output=$output}
    $exitCode = 1
} finally {
    try {
        $resolvedTemporary = [IO.Path]::GetFullPath($temporary)
        if (-not $resolvedTemporary.StartsWith(([IO.Path]::GetFullPath($temporaryParent) + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing cleanup outside Temp/TouchScrollTests.' }
        if (Test-Path -LiteralPath $resolvedTemporary) { Remove-Item -LiteralPath $resolvedTemporary -Recurse -Force }
        if ((Test-Path -LiteralPath $temporaryParent) -and @(Get-ChildItem -LiteralPath $temporaryParent -Force).Count -eq 0) { Remove-Item -LiteralPath $temporaryParent -Force }
        $temporaryFilesRemoved = $true
    } catch {
        $report = [ordered]@{result='FAIL';checkedAtUtc=[datetime]::UtcNow.ToString('o');error=('Temporary test cleanup failed: ' + $_.Exception.Message)}
        $exitCode = 1
    }
}
$clock.Stop()
$report['durationSeconds'] = $clock.Elapsed.TotalSeconds
$report['temporaryFilesRemoved'] = $temporaryFilesRemoved
[IO.Directory]::CreateDirectory((Split-Path -Parent $evidence)) | Out-Null
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidence -Encoding utf8
exit $exitCode
