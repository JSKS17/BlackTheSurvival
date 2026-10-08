using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lumia.Editor
{
    public class LumiaFontImporter : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (assetPath != "Assets/Resources/Lumia/Galmuri11.ttf") return;
            var importer = assetImporter as TrueTypeFontImporter;
            if (importer != null) importer.fontRenderingMode = FontRenderingMode.HintedRaster;
        }
    }

    public class LumiaPixelImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Lumia/", System.StringComparison.Ordinal)) return;
            PixelImportRules.Apply((TextureImporter)assetImporter, assetPath);
        }
    }

    static class PixelImportRules
    {
        public static bool Apply(TextureImporter importer, string path)
        {
            bool readable = path.Contains("Atlas");
            bool changed = importer.textureType != TextureImporterType.Default || importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                importer.isReadable != readable || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None || importer.wrapMode != TextureWrapMode.Clamp || importer.anisoLevel != 0 ||
                importer.maxTextureSize != 4096 || !importer.alphaIsTransparency || importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.sRGBTexture;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.isReadable = readable;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 0;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            return changed;
        }
    }

    public static class LumiaProjectSetup
    {
        [MenuItem("Black The Survival/Play game")]
        public static void Play() { EditorApplication.isPlaying = true; }

        [MenuItem("Black The Survival/Build Windows game")]
        public static void BuildWindows()
        {
            ConfigurePixelAssets();
            Directory.CreateDirectory("Build/Windows");
            PlayerSettings.productName = GameIdentity.ProductName;
            PlayerSettings.companyName = GameIdentity.CompanyName;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            bool development = System.Environment.GetCommandLineArgs().Contains("-lumia-development");
            var result = BuildPipeline.BuildPlayer(scenes, "Build/Windows/" + GameIdentity.ExecutableName, BuildTarget.StandaloneWindows64, development ? BuildOptions.Development : BuildOptions.None);
            Debug.Log("LUMIA BUILD " + result.summary.result + " / errors=" + result.summary.totalErrors);
            if (Application.isBatchMode) EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Black The Survival/Configure pixel imports")]
        public static void ConfigurePixelAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Lumia" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (PixelImportRules.Apply(importer, path)) importer.SaveAndReimport();
            }
            var font = AssetImporter.GetAtPath("Assets/Resources/Lumia/Galmuri11.ttf") as TrueTypeFontImporter;
            if (font != null && font.fontRenderingMode != FontRenderingMode.HintedRaster) { font.fontRenderingMode = FontRenderingMode.HintedRaster; font.SaveAndReimport(); }
        }
    }
}
