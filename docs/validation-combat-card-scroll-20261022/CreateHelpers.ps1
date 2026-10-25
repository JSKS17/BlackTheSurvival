[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath 'C:\Users\azxar\BlackTheSurvival').ProviderPath.TrimEnd('\')
$previous = Join-Path $taskRoot 'docs\validation-mobile-fonts-20261022'
$destination = Join-Path $taskRoot 'docs\validation-combat-card-scroll-20261022'
if (-not $destination.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Helper destination outside workspace.' }
if ([IO.File]::GetAttributes($destination) -band [IO.FileAttributes]::ReparsePoint) { throw 'Helper destination must not be linked.' }
$replacements = [ordered]@{
  'validation-mobile-fonts-20261022' = 'validation-combat-card-scroll-20261022'
  'SiteMobileFonts20261022' = 'SiteCombatCardScroll20261022'
  'MobileFontsRelease20261022' = 'CombatCardScrollRelease20261022'
  'Web123' = 'Web124'
  '1.0.23' = '1.0.24'
  '722031b666e32e04bcbe18c2f59c48b21d7e46cd518796864c39f3a40c0b9fa5' = '3555cdf0a36169907fffca5dbd7b8f6befdf796fffe49d95eca4ef3abbbe1c13'
  '0c1349601a9237f220c86a97f35b69682eb0ba26' = '16d05d61ab7099b7692216e97e8de82798d47fa2'
  'versionNumber: 45' = 'versionNumber: 47'
  'version_number > 45' = 'version_number > 47'
  'version_number -le 45' = 'version_number -le 47'
  'baseline Site version 45.' = 'baseline Site version 47.'
  'mobile-fonts' = 'combat-card-scroll'
  'MobileFontsRelease' = 'CombatCardScrollRelease'
  'port = 8244' = 'port = 8245'
  '모바일 폰트' = '전투 카드 터치 스크롤'
}
$utf8 = New-Object Text.UTF8Encoding($false)
foreach ($name in @('PrepareRelease.mjs','VerifyBridge.mjs','VerifyLive.mjs','Preview.mjs','Cleanup.ps1')) {
  $source = Join-Path $previous $name
  $target = Join-Path $destination $name
  if (Test-Path -LiteralPath $target) { throw "Refusing to overwrite existing helper: $target" }
  $text = [IO.File]::ReadAllText($source)
  foreach ($entry in $replacements.GetEnumerator()) { $text = $text.Replace($entry.Key, $entry.Value) }
  [IO.File]::WriteAllText($target, $text, $utf8)
}
