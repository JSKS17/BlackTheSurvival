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
    /// <summary>Opt-in measurements of real IMGUI events with deterministic, isolated fixtures.</summary>
    public sealed class LumiaListPerformanceVerification : MonoBehaviour
    {
        public const int WarmupFrames = 20, RequiredSamples = 60;
        public static readonly string[] Views = { "inventory_top", "inventory_middle", "inventory_bottom", "upgrade_top", "upgrade_bottom", "upgrade_preview", "catalog", "camp_rest_bottom", "rare_remove_bottom", "research", "combat_history", "map" };
        [Serializable] public sealed class Sample
        {
            public string view;
            public int repaintSamples, layoutSamples, minCards, maxCards, deckCards, distinctReferences, upgradeCandidates, historyEntries, researchSubjects, mapNodes;
            public double repaintMeanMs, repaintP95Ms, layoutMeanMs, layoutP95Ms, repaintAllocatedBytes;
            public bool allocationCounterAvailable;
        }
        [Serializable] public sealed class Result
        {
            public string result, phase, version, failure;
            public string measurement = "Actual full LumiaGame OnGUI Layout/Repaint CPU duration; not whole frame time or web FPS.";
            public string allocationNote = "A zero-only native Mono allocation counter is unavailable, not evidence of allocation improvement.";
            public bool isolatedSavePath, isolatedResearchPath;
            public int warmupFrames = WarmupFrames, requiredSamples = RequiredSamples, width = 1280, height = 720, fixtureSeed = 20261017;
            public List<Sample> samples = new List<Sample>();
        }
        static readonly List<double> repaint = new List<double>(), layout = new List<double>();
        static long repaintBytes;
        static bool collecting, allocationAvailable;
        static int currentCards, minCards, maxCards;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-list-perf-verify") >= 0)
                new GameObject("List performance verification").AddComponent<LumiaListPerformanceVerification>();
        }
        public static void BeginGUIEvent() { currentCards = 0; }
        // DrawCard calls this only in development builds. Count each actually rendered card.
        public static void RecordCard() { if (collecting) currentCards++; }
        public static void Record(EventType kind, long start, long allocationStart)
        {
            if (!collecting) return;
            double elapsed = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
            long bytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocationStart);
            if (kind == EventType.Repaint)
            {
                repaint.Add(elapsed); repaintBytes += bytes; allocationAvailable |= bytes > 0;
                minCards = Math.Min(minCards, currentCards); maxCards = Math.Max(maxCards, currentCards);
            }
            else if (kind == EventType.Layout) layout.Add(elapsed);
        }
        static double P95(List<double> samples)
        {
            var ordered = samples.OrderBy(value => value).ToArray();
            return ordered[(int)Math.Floor((ordered.Length - 1) * .95)];
        }
        IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-bts-list-perf-phase");
            string phase = index >= 0 && index + 1 < args.Length ? args[index + 1] : "optimized";
            if (phase != "baseline" && phase != "optimized") { Debug.LogError("Invalid list performance phase."); Application.Quit(1); yield break; }
            var result = new Result { result = "RUNNING", phase = phase, version = Application.version };
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ListPerformance"));
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, phase + ".json");
            var steps = Verify(result, directory); bool failed = false;
            while (!failed)
            {
                bool more = false; object next = null;
                try { more = steps.MoveNext(); if (more) next = steps.Current; }
                catch (Exception ex) { failed = true; collecting = false; result.result = "FAIL"; result.failure = ex.ToString(); Debug.LogException(ex); }
                if (!more || failed) break;
                yield return next;
            }
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log("LIST PERFORMANCE " + result.result + " / " + phase + " / " + output);
            if (Array.IndexOf(args, "-bts-list-perf-interactive") < 0 || failed) Application.Quit(failed ? 1 : 0);
        }
        IEnumerator Verify(Result result, string directory)
        {
            Screen.SetResolution(result.width, result.height, FullScreenMode.Windowed);
            yield return null;
            var game = LumiaGame.Instance;
            float deadline = Time.realtimeSinceStartup + 25;
            while (!game.AdvancedCardVerificationStylesReady && Time.realtimeSinceStartup < deadline) yield return null;
            game.InitializeListPerformanceVerification();
            result.isolatedSavePath = result.isolatedResearchPath = true;
            foreach (string view in Views)
            {
                game.ShowListPerformanceView(view);
                for (int i = 0; i < WarmupFrames; i++) yield return null;
                repaint.Clear(); layout.Clear(); repaintBytes = 0; minCards = int.MaxValue; maxCards = 0;
                allocationAvailable = false; collecting = true;
                deadline = Time.realtimeSinceStartup + 120;
                while ((repaint.Count < RequiredSamples || layout.Count < RequiredSamples) && Time.realtimeSinceStartup < deadline) yield return null;
                collecting = false;
                if (repaint.Count < RequiredSamples || layout.Count < RequiredSamples) throw new InvalidOperationException("Missing real GUI samples: " + view + " / repaint=" + repaint.Count + " / layout=" + layout.Count + " / stylesReady=" + game.AdvancedCardVerificationStylesReady);
                Sample sample = game.ListPerformanceCounts(view);
                sample.repaintSamples = repaint.Count; sample.layoutSamples = layout.Count;
                sample.repaintMeanMs = repaint.Average(); sample.repaintP95Ms = P95(repaint);
                sample.layoutMeanMs = layout.Average(); sample.layoutP95Ms = P95(layout);
                sample.repaintAllocatedBytes = (double)repaintBytes / repaint.Count;
                sample.allocationCounterAvailable = allocationAvailable; sample.minCards = minCards; sample.maxCards = maxCards;
                result.samples.Add(sample);
                File.WriteAllText(Path.Combine(directory, result.phase + "-progress.json"), JsonUtility.ToJson(result, true));
                Debug.Log("LIST PERFORMANCE VIEW / " + view + " / repaint " + sample.repaintMeanMs.ToString("F3") + " ms / cards " + sample.minCards + ".." + sample.maxCards);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, result.phase + "-" + view + ".png"));
                yield return new WaitForSecondsRealtime(.15f);
            }
            result.result = "PASS";
        }
    }

    public sealed partial class LumiaGame
    {
        bool ListPerformanceVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-list-perf-verify") >= 0;
        void DrawGameUIProfiled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ListPerformanceVerificationEnabled)
            {
                EventType kind = Event.current.type;
                LumiaListPerformanceVerification.BeginGUIEvent();
                long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                DrawGameUI();
                LumiaListPerformanceVerification.Record(kind, start, allocated);
                return;
            }
#endif
            DrawGameUI();
        }
        public void GuardListPerformanceVerification()
        {
            if (!ListPerformanceVerificationEnabled || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("List performance verification requires an isolated development save.");
        }
        public void InitializeListPerformanceVerification()
        {
            GuardListPerformanceVerification();
            string path = Path.Combine(Path.GetDirectoryName(savePath), "bts-list-performance-research.json");
            var profile = new ResearchProfile { data = 1000, totalData = 2000, totalRuns = 25, totalWins = 2 };
            int i = 0;
            foreach (var character in GameDatabase.Characters)
            {
                profile.subjects.Add(new ResearchSubjectProgress { subjectId = character.id, observed = true, uniqueCompleted = i % 3 == 0, usedSkills = (character.cards ?? new string[0]).Take(i % 5).ToList() });
                if (i++ % 7 == 0) profile.completedReports.Add(character.id);
            }
            File.WriteAllText(path, JsonUtility.ToJson(profile, true));
            researchStore = new ResearchStore(path, value => JsonUtility.ToJson(value, true), json => JsonUtility.FromJson<ResearchProfile>(json));
            if (researchStore.IsReadOnly) throw new InvalidOperationException("Isolated list research fixture is invalid.");
            VerificationView("inventory");
        }
        void ResetListPerformanceFixture()
        {
            GuardListPerformanceVerification();
            CloseStoryReplay(); CloseStoryLayers(); CloseCombatInformation(); ResetAdvancedCardChoiceUI();
            lobby = catalog = inventory = settings = help = fieldInfo = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = retainSelecting = hyperloopSelecting = false;
            inspectCard = inspectTrait = inspectGear = inspectBoss = mapIntel = craftPending = "";
            inspectEnemyCard = inspectRareReward = false;
            tutorial = null; tutorialOffer = tutorialQueued = false; lobby = false; summaryMode = true;
            effects.Clear(); summaryScroll.Clear();
            scroll = inventoryScroll = upgradeCardScroll = upgradePreviewScroll = rareCardScroll = researchListScroll = mapScroll = Vector2.zero;
            researchQuery = catalogQuery = ""; catalogPage = 0; upgradeSelected = -1; upgradeBranch = "";
            campRemovalSelection.Clear(); campSelectionState = null; rareRemovalSelection.Clear(); rareCopySelection = -1; rareSelectionState = null;
            Engine = new GameEngine(20261017);
            var main = GameDatabase.Runes.First(rune => rune.main);
            Engine.SetRunes(main.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == main.tree).id);
            Engine.SelectStartingPassive(Engine.State.passiveOffers[0]);
            foreach (string id in Engine.State.draftOffers.Take(3).ToArray()) Engine.ToggleDraft(id);
            if (!Engine.BeginJourney()) throw new InvalidOperationException("List performance fixture could not begin a run.");
            StoryScenes.Skip(Engine.State);
            var state = Engine.State;
            state.deck.Clear(); state.cardUpgrades.Clear(); state.upgrades.Clear(); state.gear.Clear(); state.weaponGrantedCards.Clear();
            var definitions = GameDatabase.Cards.Where(card => card.category == "skill" && StrategyMechanics.CanUpgrade(card)).Take(72).ToArray();
            if (definitions.Length != 72) throw new InvalidOperationException("Missing stress deck definitions.");
            foreach (var card in definitions) state.deck.Add(card.id);
            for (int i = 0; i < definitions.Length; i += 6)
            {
                var card = definitions[i]; string reference = CardInstances.CreateReference(state, card.id);
                state.cardUpgrades.Add(new CardUpgradeSelection { cardId = card.id, instanceId = reference, branchId = StrategyMechanics.Options(card).First().id }); state.deck.Add(reference);
                Engine.AddRareRewardCard(definitions[i + 3].id);
            }
            state.deck.AddRange(definitions.Take(6).Select(card => card.id));
            mapAct = -1; mapRow = -99; displayedTurn = -1; toastUntil = 0;
        }
        public void ShowListPerformanceView(string view)
        {
            ResetListPerformanceFixture();
            var state = Engine.State;
            if (view.StartsWith("inventory_", StringComparison.Ordinal))
            {
                inventory = true; inventoryTab = 0;
                float bottom = Math.Max(0, Mathf.Ceil(state.deck.Distinct().Count() / 6f) * 274 - 424);
                inventoryScroll.y = view == "inventory_middle" ? bottom / 2 : view == "inventory_bottom" ? bottom : 0;
            }
            else if (view.StartsWith("upgrade_", StringComparison.Ordinal))
            {
                state.stage = RunStage.Encounter; state.cardUpgradePending = true;
                if (view == "upgrade_bottom") upgradeCardScroll.y = Math.Max(0, Mathf.Ceil(Engine.UpgradeCardIndices().Count / 3f) * 228 - 379);
                if (view == "upgrade_preview")
                {
                    int index = state.deck.FindIndex(id => id == "nia_q"); if (index < 0) index = Engine.UpgradeCardIndices()[0];
                    SelectUpgradeCard(index); upgradeBranch = Engine.CardUpgradeOptions(index).First().id;
                }
            }
            else if (view == "catalog") catalog = true;
            else if (view == "camp_rest_bottom")
            {
                state.stage = RunStage.Campfire; state.campChoice = 1; state.campRemovalsRemaining = 2;
                campSelectionState = state; campSelectionNode = state.activeNodeId;
                scroll.y = Math.Max(0, Mathf.Ceil(state.deck.Count / 6f) * 245 - 365);
            }
            else if (view == "rare_remove_bottom")
            {
                state.stage = RunStage.Encounter; state.rareRemovalPending = true;
                rareSelectionState = state; rareSelectionNode = state.activeNodeId; rareSelectionIsCopy = false;
                rareCardScroll.y = Math.Max(0, Mathf.Ceil(Engine.RareRemovalIndices().Count / 6f) * 246 - 353);
            }
            else if (view == "research") { research = true; researchSubject = "nia"; researchReadReport = false; researchListScroll.y = 43 * 33; }
            else if (view == "combat_history")
            {
                for (int i = 0; i < CombatInformation.HistoryLimit; i++)
                    state.combatHistory.Add(new CombatLogEntry { sequence = i + 1, nodeId = state.activeNodeId, act = 1, turn = i / 8 + 1, opponent = "니아", kind = i % 3 == 0 ? "effect" : "card_play", cardId = state.deck[i % state.deck.Count], enemy = i % 2 == 1, title = i % 3 == 0 ? "패시브 발동" : "기술 사용", detail = "피해를 14 주었습니다. 방어를 6 얻었습니다. 조건을 충족하여 다음 기술을 무료로 연계할 수 있습니다. 출혈을 1 부여했습니다.", damage = 14, block = 6, paidCost = 2 });
                state.combatHistorySerial = CombatInformation.HistoryLimit;
                combatHistoryOpen = historyAllBattles = true; informationFilter = informationSide = 0; combatHistoryScroll.y = 4500; expandedCombatEntries.Clear();
            }
            else if (view != "map") throw new ArgumentException("Unknown list performance view: " + view);
        }
        public LumiaListPerformanceVerification.Sample ListPerformanceCounts(string view)
        {
            GuardListPerformanceVerification();
            return new LumiaListPerformanceVerification.Sample { view = view, deckCards = Engine.State.deck.Count, distinctReferences = Engine.State.deck.Distinct().Count(), upgradeCandidates = Engine.UpgradeCardIndices().Count,
                historyEntries = Engine.State.combatHistory.Count, researchSubjects = GameDatabase.Characters.Count, mapNodes = Engine.State.map.Count };
        }
    }
}
