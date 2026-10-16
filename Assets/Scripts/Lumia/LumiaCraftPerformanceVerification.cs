using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lumia
{
    /// <summary>Opt-in native measurements with an isolated save, never normal game telemetry.</summary>
    public sealed class LumiaCraftPerformanceVerification : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public string view;
            public int repaintSamples, layoutSamples, minItems, maxItems;
            public double repaintMeanMs, repaintP95Ms, layoutMeanMs, repaintAllocatedBytes;
        }
        [Serializable] public sealed class Result
        {
            public string result, phase, version, failure;
            public bool isolatedSavePath;
            public int weaponCount;
            public List<Sample> samples = new List<Sample>();
        }
        static readonly List<double> repaint = new List<double>(), layout = new List<double>();
        static long repaintBytes;
        static int minItems, maxItems;
        static bool collecting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-craft-perf-verify") >= 0)
                new GameObject("Crafting performance verification").AddComponent<LumiaCraftPerformanceVerification>();
        }
        public static void Record(EventType kind, long start, long allocationStart, int items)
        {
            if (!collecting) return;
            double elapsed = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            if (kind == EventType.Repaint)
            {
                repaint.Add(elapsed); repaintBytes += allocated;
                minItems = Math.Min(minItems, items); maxItems = Math.Max(maxItems, items);
            }
            else if (kind == EventType.Layout) layout.Add(elapsed);
        }
        IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int phaseIndex = Array.IndexOf(args, "-bts-craft-perf-phase");
            string phase = phaseIndex >= 0 && phaseIndex + 1 < args.Length ? args[phaseIndex + 1] : "optimized";
            var result = new Result { result = "RUNNING", phase = phase, version = Application.version };
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CraftingPerformance"));
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, phase + ".json");
            var steps = Verify(result, directory); bool failed = false;
            while (!failed)
            {
                bool more = false; object next = null;
                try { more = steps.MoveNext(); if (more) next = steps.Current; }
                catch (Exception ex) { failed = true; result.result = "FAIL"; result.failure = ex.ToString(); Debug.LogException(ex); }
                if (!more || failed) break;
                yield return next;
            }
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log("CRAFT PERFORMANCE " + result.result + " / " + phase + " / " + output);
            if (Array.IndexOf(args, "-bts-craft-perf-interactive") < 0 || failed) Application.Quit(failed ? 1 : 0);
        }
        IEnumerator Verify(Result result, string directory)
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            var game = LumiaGame.Instance;
            float deadline = Time.realtimeSinceStartup + 20;
            while (!game.AdvancedCardVerificationStylesReady && Time.realtimeSinceStartup < deadline) yield return null;
            game.GuardCraftPerformanceVerification(); result.isolatedSavePath = true;
            result.weaponCount = GameDatabase.Gear.Count(g => g.slot == GearSlot.Weapon);
            foreach (string view in new[] { "top", "middle", "bottom" })
            {
                game.ShowCraftPerformanceView(view);
                for (int i = 0; i < 30; i++) yield return null;
                repaint.Clear(); layout.Clear(); repaintBytes = 0; minItems = int.MaxValue; maxItems = 0; collecting = true;
                deadline = Time.realtimeSinceStartup + 30;
                while (repaint.Count < 120 && Time.realtimeSinceStartup < deadline) yield return null;
                collecting = false;
                if (repaint.Count < 120 || layout.Count < 120) throw new InvalidOperationException("Missing real GUI frames: " + view);
                var ordered = repaint.OrderBy(x => x).ToArray();
                result.samples.Add(new Sample { view = view, repaintSamples = repaint.Count, layoutSamples = layout.Count,
                    repaintMeanMs = repaint.Average(), repaintP95Ms = ordered[(int)Math.Floor((ordered.Length - 1) * .95)],
                    layoutMeanMs = layout.Average(), repaintAllocatedBytes = (double)repaintBytes / repaint.Count, minItems = minItems, maxItems = maxItems });
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, result.phase + "-" + view + ".png"));
                yield return new WaitForSecondsRealtime(.15f);
            }
            game.ShowCraftPerformanceView("top");
            result.result = "PASS";
        }
    }

    public sealed partial class LumiaGame
    {
        bool CraftPerformanceVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-craft-perf-verify") >= 0;
        int craftPerformanceItems;
        void DrawCampProfiled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (CraftPerformanceVerificationEnabled)
            {
                var kind = Event.current.type;
                long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                DrawCamp();
                LumiaCraftPerformanceVerification.Record(kind, start, allocated, craftPerformanceItems);
                return;
            }
#endif
            DrawCamp();
        }
        public void GuardCraftPerformanceVerification()
        {
            if (!CraftPerformanceVerificationEnabled || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("Crafting verification requires an isolated save.");
        }
        public void ShowCraftPerformanceView(string view)
        {
            GuardCraftPerformanceVerification();
            VerificationView("campfire");
            craftSlot = GearSlot.Weapon; craftQuery = ""; craftTags.Clear();
            int count = GameDatabase.Gear.Count(g => g.slot == GearSlot.Weapon);
            float bottom = Math.Max(0, Mathf.Ceil(count / 3f) * 151 - 284);
            scroll = new Vector2(0, view == "middle" ? 453 : view == "bottom" ? bottom : 0);
        }
    }
}
