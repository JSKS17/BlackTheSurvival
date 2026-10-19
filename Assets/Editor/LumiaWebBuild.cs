using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lumia.Editor
{
    /// <summary>Repeatable browser build; run in an isolated project with -buildTarget WebGL.</summary>
    public static class LumiaWebBuild
    {
        [MenuItem("Black The Survival/Build Web game")]
        public static void Build()
        {
            string output = Path.GetFullPath(Argument("-lumia-web-output", "Build/Web"));
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                    throw new BuildFailedException("Install Web Build Support for Unity " + Application.unityVersion + " in Unity Hub.");

                // This game's runtime UI uses IMGUI. Input System only does not support
                // its mouse/touch compatibility events; Both must be active at editor startup.
                ValidateInputHandling();
                LumiaProjectSetup.PreserveDisabledServices();

                bool development = Environment.GetCommandLineArgs().Contains("-lumia-development");
                WebGLCompressionFormat compression;
                if (!Enum.TryParse(Argument("-lumia-web-compression", "Gzip"), true, out compression))
                    throw new BuildFailedException("Unknown web compression format.");

                PlayerSettings.productName = GameIdentity.ProductName;
                PlayerSettings.companyName = GameIdentity.CompanyName;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.runInBackground = true;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
                PlayerSettings.WebGL.template = "PROJECT:BlackTheSurvival";
                PlayerSettings.WebGL.compressionFormat = compression;
                // The fallback supports ordinary static hosts without special Content-Encoding headers.
                PlayerSettings.WebGL.decompressionFallback = compression != WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.threadsSupport = false;
                PlayerSettings.WebGL.initialMemorySize = 256;
                PlayerSettings.WebGL.maximumMemorySize = 2048;
                PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                AssetDatabase.SaveAssets();
                TacticalIconAssetValidation.Validate();
                UltimateAnimationAssetValidation.Validate();

                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                if (scenes.Length == 0) throw new BuildFailedException("No enabled scenes in Build Settings.");
                Directory.CreateDirectory(output);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = development ? BuildOptions.Development : BuildOptions.None
                });
                LumiaProjectSetup.PreserveDisabledServices();
                WriteReport(output, report, development, compression);
                Debug.Log("LUMIA WEB BUILD " + report.summary.result + " / errors=" + report.summary.totalErrors + " / output=" + output);
                if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "build-failure.txt"), exception.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        static int ActiveInputHandler()
        {
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0) throw new BuildFailedException("Cannot read Active Input Handling from Project Settings.");
            using (var serialized = new SerializedObject(settings[0]))
            {
                serialized.Update();
                SerializedProperty handler = serialized.FindProperty("activeInputHandler");
                if (handler == null) throw new BuildFailedException("Active Input Handling is missing from Project Settings.");
                return handler.intValue;
            }
        }

        static bool LegacyInputManagerEnabled
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return true;
#else
                return false;
#endif
            }
        }

        static bool InputSystemPackageEnabled
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return true;
#else
                return false;
#endif
            }
        }

        static void ValidateInputHandling()
        {
            if (ActiveInputHandler() != 2 || !LegacyInputManagerEnabled || !InputSystemPackageEnabled)
                throw new BuildFailedException("Runtime IMGUI requires Active Input Handling = Both. Restart Unity after changing this setting before building.");
        }

        static void WriteReport(string output, BuildReport report, bool development, WebGLCompressionFormat compression)
        {
            var manifest = new WebBuildManifest
            {
                result = report.summary.result.ToString(),
                unityVersion = Application.unityVersion,
                productVersion = PlayerSettings.bundleVersion,
                builtAtUtc = DateTime.UtcNow.ToString("o"),
                development = development,
                compression = compression.ToString(),
                activeInputHandler = ActiveInputHandler(),
                activeInputHandling = "Both",
                legacyInputManagerEnabled = LegacyInputManagerEnabled,
                inputSystemPackageEnabled = InputSystemPackageEnabled,
                decompressionFallback = PlayerSettings.WebGL.decompressionFallback,
                threads = PlayerSettings.WebGL.threadsSupport,
                totalBytes = report.summary.totalSize,
                durationSeconds = report.summary.totalTime.TotalSeconds,
                errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings,
                files = Directory.GetFiles(output, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith("build-manifest.json", StringComparison.OrdinalIgnoreCase))
                    .Select(path => new WebBuildFile { path = path.Substring(output.Length + 1).Replace('\\', '/'), bytes = new FileInfo(path).Length })
                    .OrderByDescending(file => file.bytes).ToArray(),
                largestAssets = report.packedAssets.SelectMany(packed => packed.contents)
                    .OrderByDescending(asset => asset.packedSize).Take(30)
                    .Select(asset => new WebBuildAsset { path = asset.sourceAssetPath, bytes = asset.packedSize }).ToArray()
            };
            File.WriteAllText(Path.Combine(output, "build-manifest.json"), JsonUtility.ToJson(manifest, true));
        }

        [Serializable] class WebBuildManifest
        {
            public string result, unityVersion, productVersion, builtAtUtc, compression, activeInputHandling;
            public bool development, decompressionFallback, threads, legacyInputManagerEnabled, inputSystemPackageEnabled;
            public ulong totalBytes;
            public double durationSeconds;
            public int errors, warnings, activeInputHandler;
            public WebBuildFile[] files;
            public WebBuildAsset[] largestAssets;
        }
        [Serializable] class WebBuildFile { public string path; public long bytes; }
        [Serializable] class WebBuildAsset { public string path; public ulong bytes; }
    }
}
