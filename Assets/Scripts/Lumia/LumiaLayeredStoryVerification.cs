using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in native audit of layered stories, retained progression and mode-specific critical UI.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class LumiaLayeredStoryVerification : MonoBehaviour
    {
        [Serializable] public sealed class FileHash
        {
            public string file, sha256;
        }
        [Serializable] public sealed class Screenshot
        {
            public string view, sha256;
            public int width, height, bytes;
        }
        [Serializable] public sealed class Result
        {
            public string version, applicationVersion, result = "RUNNING", failure;
            public int width = 1280, height = 720, screenshots, storyScenes, storyFrames;
            public int backgroundAssets, characterAssets, textMeasurements, criticalViews;
            public bool isolatedSave, pointFilteredAssets, storyResumed, replayReadOnly, personalSavesUnchanged;
            public List<string> views = new List<string>();
            public List<Screenshot> images = new List<Screenshot>();
            public List<FileHash> personalBefore = new List<FileHash>(), personalAfter = new List<FileHash>();
        }

        readonly Result result = new Result();
        string output;
        bool failed;
        static readonly string[] CriticalViews =
        {
            "critical_normal_detail", "critical_hard_detail", "critical_endless_detail", "critical_endless_field",
            "critical_endless_preview", "critical_endless_gear", "critical_endless_help", "critical_endless_hit"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-layered-story-verify") >= 0)
                new GameObject("BTS layered-story verification").AddComponent<LumiaLayeredStoryVerification>();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LayeredStoryVerification"));
            Directory.CreateDirectory(output);
            result.version = result.applicationVersion = Application.version;
            SaveResult();
            // Flatten nested verification coroutines so capture/load failures also reach results.json.
            var stack = new Stack<IEnumerator>(); stack.Push(Verify());
            while (stack.Count > 0 && !failed)
            {
                object next = null; bool more = false;
                try
                {
                    more = stack.Peek().MoveNext();
                    if (more) next = stack.Peek().Current;
                    else stack.Pop();
                }
                catch (Exception exception) { Fail(exception); }
                if (failed) break;
                if (!more) continue;
                var nested = next as IEnumerator;
                if (nested != null) stack.Push(nested);
                else yield return next;
            }
            if (failed) Application.Quit(1);
        }

        IEnumerator Verify()
        {
            result.personalBefore = HashPersonal();
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            var game = LumiaGame.Instance;
            Require(game != null, "The normal game bootstrap must be present.");
            float guiDeadline = Time.realtimeSinceStartup + 5;
            while (!game.LayeredStoryStylesReady && Time.realtimeSinceStartup < guiDeadline) yield return null;
            Require(game.LayeredStoryStylesReady, "Native verification requires a visible game window to initialize OnGUI.");
            Require(Screen.width == 1280 && Screen.height == 720, "Native capture resolution must remain 1280 x 720.");
            game.GuardLayeredStoryVerification(); result.isolatedSave = true;
            game.PrepareLayeredStoryVerification();
            result.textMeasurements = game.VerifyLayeredStoryLayout();
            Require(result.textMeasurements > 0, "The native story layout audit must measure real GUI text.");
            result.backgroundAssets = VerifyAssets("Lumia/Story/Backgrounds", 6);
            result.characterAssets = VerifyAssets("Lumia/Story/Characters", 12);
            result.pointFilteredAssets = true;
            result.storyScenes = StoryScenes.All.Length;
            result.storyFrames = StoryScenes.All.Sum(scene => scene.frames.Length);
            Require(result.storyScenes == 3 && result.storyFrames == 12, "All three saved story IDs and twelve frame positions must remain available.");
            game.VerifyLayeredStoryPersistence(); result.storyResumed = result.replayReadOnly = true;

            foreach (var scene in StoryScenes.All)
            for (int frame = 0; frame < scene.frames.Length; frame++)
            {
                game.ShowStoryVerification(scene.id, frame);
                yield return Capture(scene.id + "_" + (frame + 1));
            }
            game.ShowStoryVerification("menu", 0);
            yield return Capture("story_replay_menu");
            foreach (string view in CriticalViews)
            {
                game.VerificationCriticalInformation(view);
                yield return Capture(view);
                result.criticalViews++;
            }
            Require(result.screenshots == 21 && result.views.Distinct().Count() == 21, "Native coverage must contain twelve stories, a replay menu and eight critical screens.");
            Require(result.images.All(x => x.width == 1280 && x.height == 720), "Every PNG must preserve native resolution.");
            result.personalAfter = HashPersonal();
            Require(result.personalBefore.All(before => result.personalAfter.Any(after => after.file == before.file && after.sha256 == before.sha256)),
                "A personal run/research save changed during isolated layered-story verification.");
            result.personalSavesUnchanged = true; result.result = "PASS";
            SaveResult();
            Debug.Log("BTS LAYERED STORY PASS / version=" + Application.version + " / storyFrames=" + result.storyFrames
                + " / backgroundAssets=" + result.backgroundAssets + " / characterAssets=" + result.characterAssets
                + " / text=" + result.textMeasurements + " / screenshots=" + result.screenshots + " / criticalViews=" + result.criticalViews
                + " / persistence=PASS / replayReadOnly=PASS / personalSaves=PASS");
            Application.Quit(0);
        }

        IEnumerator Capture(string view)
        {
            Require(Screen.width == 1280 && Screen.height == 720, "Capture size changed: " + view);
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(output, view + ".png");
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.12f);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            Require(File.Exists(path), "Native screenshot was not written: " + view);
            byte[] bytes = File.ReadAllBytes(path);
            Require(bytes.Length > 24 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71,
                "Screenshot is not a complete PNG: " + view);
            int width = ReadBigEndian(bytes, 16), height = ReadBigEndian(bytes, 20);
            Require(width == 1280 && height == 720, "Screenshot PNG dimensions differ from the real UI: " + view);
            result.images.Add(new Screenshot { view = view, width = width, height = height, bytes = bytes.Length, sha256 = Hash(bytes) });
            result.views.Add(view); result.screenshots++;
            SaveResult();
        }

        static int ReadBigEndian(byte[] bytes, int offset)
        { return bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]; }
        static int VerifyAssets(string path, int expected)
        {
            var textures = Resources.LoadAll<Texture2D>(path);
            Require(textures.Length == expected, "Layered resource count mismatch: " + path + " / " + textures.Length + " expected " + expected);
            Require(textures.All(texture => texture && texture.width > 0 && texture.height > 0 && texture.filterMode == FilterMode.Point),
                "Layered story assets must be present and point filtered: " + path);
            return textures.Length;
        }
        static List<FileHash> HashPersonal()
        {
            var hashes = new List<FileHash>();
            foreach (string name in new[] { GameIdentity.SaveFileName, GameIdentity.SaveFileName + ".bak", "bts-research-v1.json", "bts-research-v1.json.bak" })
            {
                string path = Path.Combine(Application.persistentDataPath, name);
                hashes.Add(new FileHash { file = name, sha256 = File.Exists(path) ? Hash(File.ReadAllBytes(path)) : "MISSING" });
            }
            return hashes;
        }
        static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void SaveResult() { File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(result, true)); }
        void Fail(Exception exception)
        {
            failed = true; result.result = "FAIL"; result.failure = exception.ToString();
            try { result.personalAfter = HashPersonal(); SaveResult(); }
            catch (Exception writeException) { Debug.LogError("BTS LAYERED STORY results write failed: " + writeException); }
            Debug.LogException(exception);
        }
    }
}
