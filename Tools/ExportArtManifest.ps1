param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot 'Temp/ArtManifestExport'
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$sourcePath = Join-Path $outputDirectory 'ExportArtManifest.cs'
$executablePath = Join-Path $outputDirectory 'ExportArtManifest.exe'
$manifestPath = Join-Path $projectRoot 'docs/art-chibi/game-art-manifest.json'
[IO.Directory]::CreateDirectory((Split-Path -Parent $manifestPath)) | Out-Null
$sourceText = @'
using System;
using System.IO;
using System.Text;
using System.Globalization;
using Lumia;
public static class ExportArtManifest
{
    static string Q(string value)
    {
        var b = new StringBuilder("\"");
        foreach (char c in value ?? "")
        {
            if (c == '"') b.Append("\\\"");
            else if (c == '\\') b.Append("\\\\");
            else if (c == '\n') b.Append("\\n");
            else if (c == '\r') b.Append("\\r");
            else if (c == '\t') b.Append("\\t");
            else if (c < 32) b.Append("\\u" + ((int)c).ToString("x4"));
            else b.Append(c);
        }
        return b.Append('"').ToString();
    }
    public static void Main(string[] args)
    {
        var b = new StringBuilder("{\n  \"schema\": 1,\n  \"sprites\": [\n    {\"id\":\"hana\",\"name\":\"하나\"}");
        foreach (var character in GameDatabase.Characters)
            b.Append(",\n    {\"id\":" + Q(character.id) + ",\"name\":" + Q(character.name) + "}");
        b.Append("\n  ],\n  \"cards\": [");
        bool first = true;
        foreach (var card in GameDatabase.Cards)
        {
            if (!first) b.Append(','); first = false;
            b.Append("\n    {\"id\":" + Q(card.id) + ",\"name\":" + Q(card.name) + ",\"owner\":" + Q(card.owner) + ",\"key\":" + Q(card.key) + ",\"category\":" + Q(card.category) + ",\"description\":" + Q(card.description) + ",\"cooldown\":" + card.cooldown.ToString(CultureInfo.InvariantCulture) + ",\"cost\":" + card.cost + ",\"effect\":" + Q(card.effect) + "}");
        }
        b.Append("\n  ],\n  \"passives\": ["); first = true;
        foreach (var passive in GameDatabase.Passives)
        {
            if (!first) b.Append(','); first = false;
            b.Append("\n    {\"id\":" + Q(passive.id) + ",\"name\":" + Q(passive.name) + ",\"owner\":" + Q(passive.owner) + ",\"description\":" + Q(passive.description) + "}");
        }
        b.Append("\n  ]\n}\n");
        File.WriteAllText(args[0], b.ToString(), new UTF8Encoding(false));
        Console.WriteLine("Art manifest exported: sprites=" + (GameDatabase.Characters.Count + 1) + ", cards=" + GameDatabase.Cards.Count + ", passives=" + GameDatabase.Passives.Count);
    }
}
'@
[IO.File]::WriteAllText($sourcePath, $sourceText, [Text.UTF8Encoding]::new($false))
$monoLibrary = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5'
$runtimePath = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compilerPath = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$arguments = @($compilerPath, '/nologo', '/target:exe', '/langversion:latest', ('/out:' + $executablePath), '/nostdlib+', ('/r:' + (Join-Path $monoLibrary 'mscorlib.dll')), ('/r:' + (Join-Path $monoLibrary 'System.dll')), ('/r:' + (Join-Path $monoLibrary 'System.Core.dll')), $sourcePath)
foreach ($dataFile in @('DataModels.cs', 'GameDatabase.cs', 'CardPresentation.cs', 'ClassicRoster.cs', 'RecentRoster.cs', 'SkillMechanics.cs', 'SkillIdentityCore.cs', 'SkillIdentityClassic.cs', 'SkillIdentityRecent.cs', 'TraitMechanics.cs', 'TraitPresentation.cs', 'PassiveIdentityCore.cs', 'PassiveIdentityClassic.cs', 'PassiveIdentityRecent.cs', 'RuneIdentityCore.cs','StatusMechanics.cs','StatusIdentity.cs','EventIdentity.cs','EventPresentation.cs','DescriptionSummary.cs','WeaponIdentity.cs','EquipmentIdentity.cs')) {
    $arguments += Join-Path $projectRoot ('Assets/Scripts/Lumia/' + $dataFile)
}
& $runtimePath @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe') $executablePath $manifestPath
exit $LASTEXITCODE
