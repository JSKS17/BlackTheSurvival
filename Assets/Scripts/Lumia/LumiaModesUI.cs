using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool modeSelecting, runMenu, endlessConfirm;
        RunMode selectedMode = RunMode.Normal;
        int lobbyDialogueIndex = -1;
        LobbyDialogueShuffle lobbyDialogueShuffle = new LobbyDialogueShuffle(Guid.NewGuid().GetHashCode());
        static readonly Rect LobbyDialogueBox = new Rect(782, 516, 430, 136);
        static readonly Rect LobbyDialogueText = new Rect(803, 553, 388, 58);

        void DrawLobbyDialogue()
        {
            if (lobbyDialogueIndex < 0) AdvanceLobbyDialogue();
            bool hover = LobbyDialogueBox.Contains(Event.current.mousePosition) && topLayer == 0;
            Box(LobbyDialogueBox, new Color(.025f, .06f, .09f, .94f), hover ? Pink : Line);
            Txt(new Rect(803, 528, 320, 22), "니아  /  GAME MASTER", 14, Pink);
            Para(LobbyDialogueText, LobbyDialogue.At(lobbyDialogueIndex), 13, Text);
            Txt(new Rect(803, 624, 388, 17), "터치하면 무작위 대사  >", 11, hover ? Pink : Muted);
            if (Hit(LobbyDialogueBox)) AdvanceLobbyDialogue();
        }

        void AdvanceLobbyDialogue() { lobbyDialogueIndex = lobbyDialogueShuffle.Next(); }

        static string ModeUnlockRequirement(RunMode mode)
        {
            return mode == RunMode.Hard ? "노말 모드에서 탈출하면 해금됩니다."
                : mode == RunMode.Endless ? "하드 모드에서 탈출하면 해금됩니다." : "처음부터 플레이할 수 있습니다.";
        }

        void ShowModeSelection()
        {
            modeSelecting = true; selectedMode = RunMode.Normal;
            runMenu = endlessConfirm = false;
        }

        bool SelectMode(RunMode mode)
        {
            if (!modeSelecting || !Enum.IsDefined(typeof(RunMode), mode) || !RunModes.IsUnlocked(researchStore.Profile, mode)) return false;
            selectedMode = mode; return true;
        }

        void CancelModeSelection() { modeSelecting = false; }
        bool ConfirmModeSelection() { return modeSelecting && NewRun(selectedMode); }

        void DrawModeDialogs()
        {
            if (modeSelecting) DrawModeSelection();
            else if (endlessConfirm) DrawEndlessConfirmation();
            else if (runMenu) DrawRunMenu();
        }

        void DrawModeSelection()
        {
            Modal(new Rect(92, 90, 1096, 555));
            Txt(new Rect(122, 111, 1036, 38), "새로운 실험 / 모드 선택", 27, Mint);
            Txt(new Rect(122, 158, 1036, 24), "모드를 선택한 뒤 시작을 눌러주세요. 취소하면 진행 중인 원정을 이어갈 수 있습니다.", 14, Muted);
            for (int index = 0; index < 3; index++)
            {
                var mode = (RunMode)index;
                bool unlocked = RunModes.IsUnlocked(researchStore.Profile, mode), chosen = selectedMode == mode;
                var card = new Rect(122 + index * 348, 201, 340, 326);
                Color tone = mode == RunMode.Normal ? Mint : mode == RunMode.Hard ? Gold : Pink;
                Box(card, chosen ? C("213630") : Panel, chosen ? tone : Line);
                Txt(new Rect(card.x + 20, card.y + 15, 298, 25), "0" + (index + 1) + "  /  " + RunModes.Name(mode), 23, unlocked ? tone : Muted);
                Txt(new Rect(card.x + 20, card.y + 54, 298, 20), unlocked ? chosen ? "선택됨" : "플레이 가능" : "잠금", 12, unlocked ? tone : Muted);
                Para(new Rect(card.x + 20, card.y + 91, 298, 181), RunModes.Description(mode), 13, unlocked ? Text : Muted);
                Txt(new Rect(card.x + 20, card.y + 289, 298, 20), unlocked ? mode == RunMode.Endless ? "종료 시 관측 기록 정산" : "3막 탈출에 도전" : ModeUnlockRequirement(mode), 11, unlocked ? tone : Muted);
                if (Hit(card, unlocked)) SelectMode(mode);
            }
            if (Btn(new Rect(122, 548, 340, 45), "취소 / 로비로", false, Muted)) CancelModeSelection();
            if (Btn(new Rect(478, 548, 680, 45), RunModes.Name(selectedMode) + " 모드로 시작  >", true, Mint, RunModes.IsUnlocked(researchStore.Profile, selectedMode))) ConfirmModeSelection();
            Txt(new Rect(122, 611, 1036, 19), "시작을 확정하면 새 원정으로 교체됩니다. 완료한 연구 기록과 모드 해금은 보존됩니다.", 12, Muted);
        }

        void DrawModeBadge(Rect rect)
        {
            if (Engine == null) return;
            var mode = RunModes.ModeFor(Engine.State);
            Color tone = mode == RunMode.Normal ? Mint : mode == RunMode.Hard ? Gold : Pink;
            Box(rect, Panel, tone);
            Txt(rect, RunModes.CycleLabel(Engine.State), 10, tone, true);
        }

        void OpenRunMenu()
        {
            if (Engine != null && RunModes.ModeFor(Engine.State) == RunMode.Endless
                && Engine.State.stage != RunStage.Won && Engine.State.stage != RunStage.Lost)
            {
                runMenu = true; endlessConfirm = false;
            }
            else { lobby = true; Save(); }
        }

        bool CanRetireEndless => Engine != null && RunModes.ModeFor(Engine.State) == RunMode.Endless
            && Engine.State.stage != RunStage.Preparation && Engine.State.stage != RunStage.Won && Engine.State.stage != RunStage.Lost;

        void DrawRunMenu()
        {
            Modal(new Rect(390, 158, 500, 412));
            Txt(new Rect(418, 183, 444, 36), "원정 메뉴 / " + RunModes.CycleLabel(Engine.State), 24, Pink, true);
            Para(new Rect(418, 235, 444, 50), "잠시 멈추어 원정을 계속하거나, 기록을 저장하고 로비로 돌아갈 수 있습니다.", 15, Text);
            if (Btn(new Rect(418, 305, 444, 48), "원정 계속", true, Mint)) runMenu = false;
            if (Btn(new Rect(418, 369, 444, 48), "저장하고 로비로", false, Muted)) SaveAndReturnToLobby();
            if (Btn(new Rect(418, 433, 444, 48), "무한 원정 정산", false, Gold, CanRetireEndless)) { runMenu = false; endlessConfirm = true; }
            Txt(new Rect(418, 510, 444, 25), CanRetireEndless ? "정산 후 종료는 패배와 구분되어 기록됩니다." : "원정 준비를 마치면 정산할 수 있습니다.", 12, Muted, true);
        }

        void SaveAndReturnToLobby() { runMenu = false; lobby = true; Save(); }

        void DrawEndlessConfirmation()
        {
            Modal(new Rect(365, 203, 550, 315));
            Txt(new Rect(394, 226, 492, 36), "무한 원정을 정산할까요?", 24, Gold, true);
            Para(new Rect(394, 282, 492, 101), "현재 원정을 자발적으로 종료하고 관측 데이터와 연구 성과를 보존합니다.\n정산한 원정은 이어서 플레이할 수 없습니다.", 16, Text);
            if (Btn(new Rect(394, 423, 232, 52), "취소 / 원정 계속", false, Muted)) { endlessConfirm = false; runMenu = false; }
            if (Btn(new Rect(642, 423, 244, 52), "정산하고 종료", true, Gold, CanRetireEndless)) ConfirmEndlessRetirement();
        }

        bool ConfirmEndlessRetirement()
        {
            if (!endlessConfirm || !CanRetireEndless || !Engine.EndEndlessRun()) return false;
            endlessConfirm = runMenu = false; lobby = false; effects.Clear(); Engine.CombatActions.Clear();
            Save(); return true;
        }

        static string ModeEndingTitle(RunState state)
        {
            return state.endlessRetired ? "EXPEDITION SETTLED" : state.stage == RunStage.Won ? "EXPERIMENT COMPLETE" : "SIGNAL LOST";
        }

        string ModeEndingSubtitle(RunState state)
        {
            if (state.endlessRetired) return "무한 원정 정산 · 자발 종료 · 관측 기록 보존";
            if (state.stage != RunStage.Won) return "원정 종료 · 관측 기록 보존";
            if (RunModes.ModeFor(state) == RunMode.Hard)
                return RunModes.IsUnlocked(researchStore.Profile, RunMode.Endless) ? "하드 탈출 성공 · 무한 모드 해금" : "하드 탈출 성공 · 연구 정산 후 무한 모드 해금";
            return RunModes.IsUnlocked(researchStore.Profile, RunMode.Hard) ? "노말 탈출 성공 · 하드 모드 해금" : "노말 탈출 성공 · 연구 정산 후 하드 모드 해금";
        }

        static string ModeEndingSummary(RunState state)
        {
            return RunModes.CycleLabel(state) + " 모드  /  최종 레벨 " + state.level + "  /  덱 " + state.deck.Count
                + "장  /  크레딧 " + state.credits + "  /  보스 " + state.bossVictories + "명";
        }

        bool ModesVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-modes-verify") >= 0;

        public string[] ModesVerificationViews => new[]
        {
            "modes_lobby_dialogue", "modes_lobby_longest", "modes_locked", "modes_cancelled", "modes_normal_preparation", "modes_normal_map",
            "modes_normal_cleared", "modes_hard_unlocked", "modes_hard_combat", "modes_hard_cleared", "modes_endless_unlocked", "modes_endless_cycle",
            "modes_endless_menu", "modes_endless_confirm", "modes_endless_retired", "modes_endless_reloaded"
        };

        void GuardModesVerification()
        {
            if (!(ModesVerificationEnabled || AugmentVerificationEnabled) || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("Mode verification must use the isolated development run and research slots.");
        }

        void ResetModesProfile(bool normal = false, bool hard = false)
        {
            GuardModesVerification();
            foreach (string pending in PendingResearchFiles()) File.Delete(pending);
            string path = Path.Combine(Application.persistentDataPath, "bts-research-verification.json");
            File.WriteAllText(path, JsonUtility.ToJson(new ResearchProfile { normalCleared = normal, hardCleared = hard }, true));
            researchStore = new ResearchStore(path, profile => JsonUtility.ToJson(profile, true), json => JsonUtility.FromJson<ResearchProfile>(json));
            pendingResearchRuns = 0; researchSaveError = "";
        }

        GameEngine ModesFixtureRun(int seed, RunMode mode)
        {
            var engine = new GameEngine(seed, mode);
            engine.SetResearchUnlocks(researchStore.Profile.completedReports);
            var main = GameDatabase.Runes.First(rune => rune.main);
            engine.SetRunes(main.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == main.tree).id);
            engine.SelectStartingPassive(engine.State.passiveOffers[0]);
            foreach (string id in engine.State.draftOffers.Take(3).ToArray()) engine.ToggleDraft(id);
            if (!engine.BeginJourney()) throw new InvalidOperationException("Mode native fixture could not begin its journey.");
            return engine;
        }

        void CompleteModesBoss(int act, int lane, bool finishAugment = true)
        {
            var node = EnterBossPathsBattle(act, lane); var state = Engine.State; var combat = state.combat;
            combat.enemyHp = 1; combat.enemyBlock = combat.enemyEvasion = 0; combat.enemyGear.Clear(); combat.enemyPassiveId = "";
            combat.enemyTraits = new TraitActorState(); combat.enemySkills = new SkillActorState(); combat.enemyStatuses = new StatusActorState();
            combat.hand = new List<string> { "basic_attack" }; combat.energy = 200;
            if (!Engine.PlayCard(0) || state.stage != RunStage.Rewards || !Engine.FinishRewards() || !node.visited)
                throw new InvalidOperationException("Mode native fixture must defeat an entered boss with an actual card and commit its rewards.");
            if (state.stage == RunStage.AugmentChoice && finishAugment && !Engine.SkipAugment())
                throw new InvalidOperationException("A legacy mode fixture must explicitly skip its new Endless augment choice.");
            if(state.stage==RunStage.EquipmentSlotChoice && finishAugment
                && (!ToggleEquipmentSlotChoice(GearSlot.Weapon)||!FinishEquipmentSlotChoice()))
                throw new InvalidOperationException("An Endless mode fixture must explicitly confirm its equipment expansion before the next cycle.");
            var expected = act == 3 ? state.mode != RunMode.Endless ? RunStage.Won : finishAugment ? RunStage.Map : RunStage.AugmentChoice : RunStage.Map;
            if (state.stage != expected)
                throw new InvalidOperationException("The third boss must escape in finite modes and offer an augment before the next Endless cycle.");
            effects.Clear(); Engine.CombatActions.Clear();
        }

        void PrepareModesVictory(RunMode mode)
        {
            Engine = ModesFixtureRun(80110 + (int)mode, mode); lobby = false;
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1);
            Save();
        }

        public void VerificationModesFlow()
        {
            if (!(ModesVerificationEnabled || AugmentVerificationEnabled)) return;
            GuardModesVerification(); ResetModesProfile();
            Engine = ModesFixtureRun(80101, RunMode.Normal); lobby = true; Save();
            string before = JsonUtility.ToJson(Engine.State), disk = File.ReadAllText(savePath), profile = JsonUtility.ToJson(researchStore.Profile);
            int rng = Engine.State.rngState, previous = -1; var seen = new HashSet<int>();
            // Start at the boundary of a fresh shuffle for this isolated UI check.
            lobbyDialogueShuffle = new LobbyDialogueShuffle(80210);
            for (int index = 0; index < LobbyDialogue.Count; index++)
            {
                AdvanceLobbyDialogue();
                if (!seen.Add(lobbyDialogueIndex) || lobbyDialogueIndex == previous || string.IsNullOrWhiteSpace(LobbyDialogue.At(lobbyDialogueIndex)))
                    throw new InvalidOperationException("Lobby dialogue must visit every unique line without immediate repetition.");
                previous = lobbyDialogueIndex;
            }
            AdvanceLobbyDialogue();
            if (lobbyDialogueIndex == previous || LobbyDialogue.Count < 3) throw new InvalidOperationException("Lobby dialogue must reshuffle without an immediate repeat.");
            ShowModeSelection();
            if (SelectMode(RunMode.Hard) || SelectMode(RunMode.Endless)) throw new InvalidOperationException("Locked modes must not be selectable.");
            selectedMode = RunMode.Hard;
            if (ConfirmModeSelection()) throw new InvalidOperationException("Confirmation must independently block a locked mode.");
            CancelModeSelection();
            if (before != JsonUtility.ToJson(Engine.State) || disk != File.ReadAllText(savePath) || profile != JsonUtility.ToJson(researchStore.Profile) || rng != Engine.State.rngState)
                throw new InvalidOperationException("Dialogue and cancelled mode selection must not mutate the run, research, disk or RNG.");
            ShowModeSelection();
            if (!ConfirmModeSelection() || Engine.State.mode != RunMode.Normal || Engine.State.stage != RunStage.Preparation)
                throw new InvalidOperationException("Only mode confirmation may replace the saved run.");
            PrepareModesVictory(RunMode.Normal);
            if (!researchStore.Profile.normalCleared || researchStore.Profile.hardCleared || !RunModes.IsUnlocked(researchStore.Profile, RunMode.Hard)
                || RunModes.IsUnlocked(researchStore.Profile, RunMode.Endless)) throw new InvalidOperationException("Normal escape must unlock only Hard.");
            ShowModeSelection();
            if (!SelectMode(RunMode.Hard) || !ConfirmModeSelection() || Engine.State.mode != RunMode.Hard) throw new InvalidOperationException("Unlocked Hard must start through mode confirmation.");
            PrepareModesVictory(RunMode.Hard);
            if (!researchStore.Profile.hardCleared || !RunModes.IsUnlocked(researchStore.Profile, RunMode.Endless)) throw new InvalidOperationException("Hard escape must unlock Endless.");
            ShowModeSelection();
            if (!SelectMode(RunMode.Endless) || !ConfirmModeSelection() || Engine.State.mode != RunMode.Endless) throw new InvalidOperationException("Unlocked Endless must start through mode confirmation.");
            Engine = ModesFixtureRun(80103, RunMode.Endless); lobby = false;
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1);
            if (Engine.State.endlessCycle != 1 || Engine.State.act != 1 || Engine.State.stage != RunStage.Map || Engine.State.archivedMapNodes.Count == 0 || Engine.EquipmentCapacity(GearSlot.Weapon)!=3)
                throw new InvalidOperationException("Endless must retain the previous cycle and generate its next three acts.");
            Save(); string endless = File.ReadAllText(savePath); Load();
            if (Engine.State.mode != RunMode.Endless || Engine.State.endlessCycle != 1 || Engine.State.archivedMapNodes.Count == 0 || Engine.EquipmentCapacity(GearSlot.Weapon)!=3)
                throw new InvalidOperationException("Unity run file reload must preserve mode, cycle and archived visits.");
            OpenRunMenu(); endlessConfirm = true; runMenu = false;
            endlessConfirm = false;
            if (endless != File.ReadAllText(savePath) || Engine.State.stage != RunStage.Map) throw new InvalidOperationException("Cancelling retirement must preserve the ongoing run.");
            OpenRunMenu(); endlessConfirm = true; runMenu = false;
            int hp = Engine.State.hp;
            if (!ConfirmEndlessRetirement() || !Engine.State.endlessRetired || Engine.State.hp != hp || Engine.State.stage != RunStage.Lost
                || Engine.State.research?.settled != true || Engine.State.research.settlement.escaped || Engine.State.research.settlement.mode != RunMode.Endless)
                throw new InvalidOperationException("Voluntary Endless retirement must retain HP and settle once without an escape bonus.");
            int data = researchStore.Profile.data, runs = researchStore.Profile.totalRuns;
            Load(); Save();
            if (!Engine.State.endlessRetired || data != researchStore.Profile.data || runs != researchStore.Profile.totalRuns)
                throw new InvalidOperationException("Retired run reload must keep its distinct result and never settle twice.");
            var legacyProfile = JsonUtility.FromJson<ResearchProfile>("{\"version\":1,\"totalRuns\":1,\"totalWins\":1}");
            ResearchProgression.Normalize(legacyProfile);
            if (!RunModes.IsUnlocked(legacyProfile, RunMode.Hard) || RunModes.IsUnlocked(legacyProfile, RunMode.Endless))
                throw new InvalidOperationException("Legacy recorded escapes must unlock Hard, but not Endless.");
            string legacyRun = Regex.Replace(JsonUtility.ToJson(ModesFixtureRun(80104, RunMode.Normal).State), "\"(?:mode|endlessCycle)\"\\s*:\\s*0\\s*,", "");
            if (Regex.IsMatch(legacyRun, "\"(?:mode|endlessCycle)\"\\s*:")) throw new InvalidOperationException("Legacy run fixture must actually omit both additive mode fields.");
            var oldEngine = new GameEngine(JsonUtility.FromJson<RunState>(legacyRun));
            if (oldEngine.State.mode != RunMode.Normal || oldEngine.State.endlessCycle != 0) throw new InvalidOperationException("Missing legacy mode fields must load as Normal.");
            modeSelecting = runMenu = endlessConfirm = false; toastUntil = 0; lobby = true;
            Debug.Log("BTS MODES FLOW PASS / dialogue=" + LobbyDialogue.Count + " / locked=PASS / cancelReadOnly=PASS / normalHardEndless=PASS / actualBosses=9 / cycleReload=PASS / retirement=PASS / settlementOnce=PASS / legacyUnityJson=PASS / isolatedSave=PASS");
        }

        public void VerificationModesLayout()
        {
            if (!ModesVerificationEnabled) return;
            if (!initialized) throw new InvalidOperationException("Mode layout audit needs the actual native GUI font.");
            int measurements = 0; float dialogueMax = 0, descriptionMax = 0;
            Action<string, int, float, float> measure = (text, size, width, height) =>
            {
                font.RequestCharactersInTexture(text, size, FontStyle.Normal);
                float actual = new GUIStyle(wrapped) { fontSize = size }.CalcHeight(new GUIContent(text), width); measurements++;
                if (actual > height + .1f) throw new InvalidOperationException("Mode or dialogue text exceeds native layout: " + text + " / " + actual + " > " + height);
            };
            foreach (string line in LobbyDialogue.Lines)
            {
                measure(line, 13, LobbyDialogueText.width, LobbyDialogueText.height);
                dialogueMax = Math.Max(dialogueMax, new GUIStyle(wrapped) { fontSize = 13 }.CalcHeight(new GUIContent(line), LobbyDialogueText.width));
            }
            for (int index = 0; index < 3; index++)
            {
                string text = RunModes.Description((RunMode)index); measure(text, 13, 298, 181);
                descriptionMax = Math.Max(descriptionMax, new GUIStyle(wrapped) { fontSize = 13 }.CalcHeight(new GUIContent(text), 298));
                measure(ModeUnlockRequirement((RunMode)index), 11, 298, 20);
            }
            foreach (int cycle in new[] { 0, 1, 99, int.MaxValue }) measure(RunModes.CycleLabel(new RunState { mode = RunMode.Endless, endlessCycle = cycle }), 10, 114, 25);
            measure("무한 원정 정산 · 자발 종료 · 관측 기록 보존", 23, 1180, 32);
            measure("노말 탈출 성공 · 연구 정산 후 하드 모드 해금", 23, 1180, 32);
            measure("하드 탈출 성공 · 연구 정산 후 무한 모드 해금", 23, 1180, 32);
            Debug.Log("BTS MODES LAYOUT PASS / measurements=" + measurements + " / dialogueMax=" + dialogueMax + " / descriptionMax=" + descriptionMax + " / nativeFont=PASS");
        }

        public void VerificationModesView(string view)
        {
            if (!ModesVerificationEnabled) return;
            GuardModesVerification();
            lobby = catalog = inventory = settings = help = fieldInfo = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = false; inspectCard = inspectTrait = inspectGear = craftPending = mapIntel = inspectBoss = "";
            effects.Clear(); toastUntil = 0; mapAct = -1; mapRow = -99; ResetBossPreviews();
            ResetModesProfile(view.Contains("hard") || view.Contains("endless"), view.Contains("endless"));
            Engine = ModesFixtureRun(80201, RunMode.Normal);
            if (view == "modes_lobby_dialogue" || view == "modes_lobby_longest")
            {
                lobby = true; lobbyDialogueIndex = view.EndsWith("longest", StringComparison.Ordinal)
                    ? Enumerable.Range(0, LobbyDialogue.Count).OrderByDescending(index => LobbyDialogue.At(index).Length).First() : 2;
                return;
            }
            if (view == "modes_locked" || view == "modes_hard_unlocked" || view == "modes_endless_unlocked")
            {
                lobby = true; ShowModeSelection();
                if (view.Contains("hard")) SelectMode(RunMode.Hard);
                if (view.Contains("endless")) SelectMode(RunMode.Endless);
                return;
            }
            if (view == "modes_cancelled") { lobby = true; Save(); ShowModeSelection(); CancelModeSelection(); return; }
            if (view == "modes_normal_preparation") { lobby = true; ShowModeSelection(); if (!ConfirmModeSelection()) throw new InvalidOperationException("Native Normal confirmation failed."); return; }
            if (view == "modes_normal_cleared" || view == "modes_hard_cleared") { PrepareModesVictory(view.Contains("hard") ? RunMode.Hard : RunMode.Normal); return; }
            if (view == "modes_normal_map") return;
            Engine = ModesFixtureRun(80202, view == "modes_hard_combat" ? RunMode.Hard : RunMode.Endless);
            if (view == "modes_hard_combat")
            {
                var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Subject; node.assignedOpponent = "nia";
                if (!Engine.EnterNode(node.lane)) throw new InvalidOperationException("Hard UI fixture could not enter its actual opponent.");
                displayedTurn = Engine.State.combat.turn; return;
            }
            for (int act = 1; act <= 3; act++) CompleteModesBoss(act, 1);
            if (view == "modes_endless_cycle") return;
            OpenRunMenu();
            if (view == "modes_endless_menu") return;
            runMenu = false; endlessConfirm = true;
            if (view == "modes_endless_confirm") return;
            if (!ConfirmEndlessRetirement()) throw new InvalidOperationException("Native retirement UI failed to settle.");
            if (view == "modes_endless_reloaded") Load();
        }
    }
}
