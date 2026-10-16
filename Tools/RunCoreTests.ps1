param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor', [switch]$Balance, [switch]$UpgradeOnly, [switch]$RareOnly, [switch]$StoryOnly, [switch]$AccuracyOnly, [switch]$WeaponOnly, [switch]$AdvancedOnly, [switch]$SuaBlindOnly, [switch]$SummaryOnly, [switch]$CraftingOnly)
$ErrorActionPreference = 'Stop'
if (@($Balance,$UpgradeOnly,$RareOnly,$StoryOnly,$AccuracyOnly,$WeaponOnly,$AdvancedOnly,$SuaBlindOnly,$SummaryOnly,$CraftingOnly).Where({$_}).Count -gt 1) { throw 'Choose only one focused test mode.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'Temp/CoreTests.exe'
[IO.Directory]::CreateDirectory((Split-Path -Parent $outputPath)) | Out-Null
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$runtimePath = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compilerPath = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$arguments = @($compilerPath, '/nologo', '/target:exe', '/define:LUMIA_TEST_RUNNER', '/langversion:latest', ('/out:' + $outputPath), '/nostdlib+', ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), (Join-Path $projectRoot 'Assets/Scripts/Lumia/DataModels.cs'), (Join-Path $projectRoot 'Assets/Scripts/Lumia/GameDatabase.cs'), (Join-Path $projectRoot 'Assets/Scripts/Lumia/GameEngine.cs'), (Join-Path $PSScriptRoot 'CoreSmokeTests.cs'), (Join-Path $PSScriptRoot 'BalanceSimulation.cs'))
foreach ($dataFile in @('CardPresentation.cs', 'ClassicRoster.cs', 'RecentRoster.cs', 'SkillMechanics.cs', 'FreeCastMechanics.cs', 'CrossSubjectSynergies.cs', 'SkillIdentityCore.cs', 'SkillIdentityClassic.cs', 'SkillIdentityRecent.cs', 'TraitMechanics.cs', 'TraitPresentation.cs', 'PassiveIdentityCore.cs', 'PassiveIdentityClassic.cs', 'PassiveIdentityRecent.cs', 'RuneIdentityCore.cs','StatusMechanics.cs','StatusIdentity.cs','EventIdentity.cs','EventPresentation.cs','EncounterChains.cs','StrategyMechanics.cs','BossMechanics.cs','BossProfiles.cs','DescriptionSummary.cs','WeaponIdentity.cs','EquipmentIdentity.cs','RareSkinCatalog.cs','RareSkinStories.cs')) {
    $dataPath = Join-Path $projectRoot ('Assets/Scripts/Lumia/' + $dataFile)
    if (Test-Path -LiteralPath $dataPath) { $arguments += $dataPath }
}
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/CardInstances.cs'
foreach ($advancedFile in @('AdvancedCardMechanics.cs','GameEngineAdvancedCards.cs','CompanionMechanics.cs','CardSummaryMechanics.cs','CraftingCatalog.cs')) { $arguments += Join-Path $projectRoot ('Assets/Scripts/Lumia/' + $advancedFile) }
foreach ($advancedTest in @('AdvancedRulesSmokeTests.cs','CompanionSmokeTests.cs','AdvancedCombatSmokeTests.cs','AdvancedCardPresentationSmokeTests.cs','SuaBlindSmokeTests.cs','CardSummaryRegressionTests.cs','CraftingCatalogSmokeTests.cs')) {
    $advancedTestPath = Join-Path $PSScriptRoot $advancedTest
    if (Test-Path -LiteralPath $advancedTestPath) { $arguments += $advancedTestPath }
}
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/AccuracyMechanics.cs'
$arguments += Join-Path $PSScriptRoot 'AccuracySmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/WeaponDamageBalance.cs'
$arguments += Join-Path $PSScriptRoot 'WeaponSpecialMechanicSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'WeaponDamageBalanceSmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/GameEngineCardInstances.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/RareCardUpgrades.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/RareEncounterEngine.cs'
$arguments += Join-Path $PSScriptRoot 'RareCardUpgradeSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'RareEncounterV2SmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'StrategySmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/RunModes.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/LobbyDialogue.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/EndlessAugments.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/EndlessAugmentEngine.cs'
$arguments += Join-Path $PSScriptRoot 'EndlessAugmentSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'CardClassificationSmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/CombatVisualProfiles.cs'
$arguments += Join-Path $PSScriptRoot 'CombatVisualSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'RunModesSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'LobbyDialogueSmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/TutorialSession.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/StoryScenes.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/StoryComposition.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/CombatInformation.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/EquipmentCritical.cs'
$arguments += Join-Path $PSScriptRoot 'TutorialSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'StoryScenesSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'CombatInformationSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'GarnetExecutionSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'FoodAndDelayedRecastSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'LayeredStorySmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'EndlessCriticalSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'EquipmentExpansionSmokeTests.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/WildlifeMechanics.cs'
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/WildlifeEngine.cs'
$arguments += Join-Path $PSScriptRoot 'WildlifeSmokeTests.cs'
$arguments += Join-Path $PSScriptRoot 'BossSmokeTests.cs'
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Lumia') -Filter 'Research*.cs' | ForEach-Object { $_.FullName })
$arguments += Join-Path $projectRoot 'Assets/Scripts/Lumia/BrowserPersistence.cs'
$arguments += @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'Research*SmokeTests.cs' | ForEach-Object { $_.FullName })
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Lumia') -Filter 'BossProfiles*.cs' | Where-Object { $_.Name -ne 'BossProfiles.cs' } | ForEach-Object { $_.FullName })
& $runtimePath @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($Balance) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--balance' }
elseif ($UpgradeOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--upgrade-only' }
elseif ($RareOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--rare-only' }
elseif ($StoryOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--story-only' }
elseif ($AccuracyOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--accuracy-only' }
elseif ($WeaponOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--weapon-only' }
elseif ($AdvancedOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--advanced-only' }
elseif ($SuaBlindOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--sua-blind-only' }
elseif ($SummaryOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--summary-only' }
elseif ($CraftingOnly) { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath '--crafting-only' }
else { & (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $outputPath }
exit $LASTEXITCODE
