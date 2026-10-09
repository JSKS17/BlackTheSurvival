param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor', [switch]$Balance)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'Temp/CoreTests.exe'
[IO.Directory]::CreateDirectory((Split-Path -Parent $outputPath)) | Out-Null
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$runtimePath = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compilerPath = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$arguments = @($compilerPath, '/nologo', '/target:exe', '/define:LUMIA_TEST_RUNNER', '/langversion:latest', ('/out:' + $outputPath), '/nostdlib+', ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), (Join-Path $projectRoot 'Assets/Scripts/Lumia/DataModels.cs'), (Join-Path $projectRoot 'Assets/Scripts/Lumia/GameDatabase.cs'), (Join-Path $projectRoot 'Assets/Scripts/Lumia/GameEngine.cs'), (Join-Path $PSScriptRoot 'CoreSmokeTests.cs'), (Join-Path $PSScriptRoot 'BalanceSimulation.cs'))
foreach ($dataFile in @('CardPresentation.cs', 'ClassicRoster.cs', 'RecentRoster.cs', 'SkillMechanics.cs', 'FreeCastMechanics.cs', 'CrossSubjectSynergies.cs', 'SkillIdentityCore.cs', 'SkillIdentityClassic.cs', 'SkillIdentityRecent.cs', 'TraitMechanics.cs', 'TraitPresentation.cs', 'PassiveIdentityCore.cs', 'PassiveIdentityClassic.cs', 'PassiveIdentityRecent.cs', 'RuneIdentityCore.cs','StatusMechanics.cs','StatusIdentity.cs','EventIdentity.cs','EventPresentation.cs','DescriptionSummary.cs','WeaponIdentity.cs','EquipmentIdentity.cs')) {
    $dataPath = Join-Path $projectRoot ('Assets/Scripts/Lumia/' + $dataFile)
    if (Test-Path -LiteralPath $dataPath) { $arguments += $dataPath }
}
& $runtimePath @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($Balance) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--balance' }
else { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath }
exit $LASTEXITCODE
