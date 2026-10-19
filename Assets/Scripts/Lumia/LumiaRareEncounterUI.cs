using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool inspectRareReward;
        readonly HashSet<int> rareRemovalSelection = new HashSet<int>();
        int rareCopySelection = -1, rareSelectionNode = -1;
        bool rareSelectionIsCopy;
        RunState rareSelectionState;
        int rareCandidateRevision = -1;
        RunState rareCandidateState;
        bool rareCandidateCopy;
        List<int> rareCandidateIndices = new List<int>();
        Vector2 rareCardScroll;
        string rareRevealKey = "";
        float rareRevealStartedAt, rareRevealVerificationTime = -1;
        const float RareRevealDelay = .4f, RareRevealDuration = 1.2f;
        bool RareV2VerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-rare-v2-verify") >= 0;

        float RareRevealElapsed()
        {
            var s = Engine.State;
            string key = s.research?.runId + "/" + s.activeNodeId + "/" + s.rareRevealEventId;
            if (rareRevealKey != key) { rareRevealKey = key; rareRevealStartedAt = Time.unscaledTime; }
            return RareV2VerificationEnabled && rareRevealVerificationTime >= 0 ? rareRevealVerificationTime : Time.unscaledTime - rareRevealStartedAt;
        }

        bool UpdateRareReveal()
        {
            var s = Engine.State;
            if (s.rareRevealSeen || string.IsNullOrEmpty(s.rareRevealEventId) || !s.encounterOffers.Contains(s.rareRevealEventId)) return false;
            if (RareRevealElapsed() >= RareRevealDelay + RareRevealDuration) { CompleteRareReveal(); return false; }
            return true;
        }

        void CompleteRareReveal()
        {
            if (Engine.State.rareRevealSeen) return;
            Engine.State.rareRevealSeen = true;
            Save();
        }

        void DrawRareAwareOffer(Rect r, EventDef actual)
        {
            var s = Engine.State;
            bool converting = actual.id == s.rareRevealEventId && !s.rareRevealSeen;
            float elapsed = converting ? RareRevealElapsed() : RareRevealDelay + RareRevealDuration;
            float progress = Mathf.Clamp01((elapsed - RareRevealDelay) / RareRevealDuration);
            var source = converting ? GameDatabase.Event(s.rareRevealSourceEventId) : null;
            var visible = source != null && progress < 1 ? source : actual;
            string rareSubject = ResearchEncounters.SubjectForEvent(actual.id);
            bool rare = rareSubject != null && (!converting || progress >= 1);
            Box(r, Panel, converting && progress > 0 ? RareTone : rare ? Gold : Line);
            Rect portrait = new Rect(r.x + 94, r.y + 22, 200, 187);
            if (converting && progress > 0 && rareSubject != null)
            {
                float zoom = 1 + .1f * Mathf.Sin(progress * Mathf.PI);
                Rect zoomed = new Rect(portrait.center.x - portrait.width * zoom / 2, portrait.center.y - portrait.height * zoom / 2, portrait.width * zoom, portrait.height * zoom);
                Tex(zoomed, PixelArt.Portrait(source?.owner ?? actual.owner), ScaleMode.ScaleToFit, new Color(1, 1, 1, 1 - progress));
                Tex(zoomed, PixelArt.RarePortrait(rareSubject), ScaleMode.ScaleToFit, new Color(1, 1, 1, progress));
                // A fixed number of square pixels keeps the flash crisp and cheap at every resolution.
                for (int i = 0; i < 24; i++)
                {
                    float phase = i * 2.399963f, radius = 16 + progress * 91 + i % 3 * 8;
                    float x = Mathf.Round((portrait.center.x + Mathf.Cos(phase) * radius) / 4) * 4;
                    float y = Mathf.Round((portrait.center.y + Mathf.Sin(phase) * radius * .75f) / 4) * 4;
                    int size = i % 3 == 0 ? 8 : 4;
                    Fill(new Rect(x, y, size, size), new Color(RareTone.r, RareTone.g, RareTone.b, Mathf.Sin(progress * Mathf.PI) * .85f));
                }
            }
            else Tex(portrait, rare ? PixelArt.RarePortrait(rareSubject) : PixelArt.Portrait(visible.owner), ScaleMode.ScaleToFit);
            Txt(new Rect(r.x + 20, r.y + 224, 347, 24), visible.owner + (rare ? "  ·  레어 조우" : converting && progress > 0 ? "  ·  신호 변환 중" : visible.chainOnly ? "  ·  약속한 재회" : ""), 14, converting && progress > 0 ? RareTone : rare ? Gold : Mint, true);
            if (rare) FitText(new Rect(r.x + 20, r.y + 247, 347, 17), RareSkinCatalog.NameForSubject(rareSubject), 11, Gold);
            Txt(new Rect(r.x + 20, r.y + 264, 347, 32), visible.title, 20, Text, true);
            FitText(new Rect(r.x + 25, r.y + 313, 337, 65), rare ? RareSkinStories.Preview(actual.story) : converting && progress > 0 ? "평범한 신호 너머에서, 다른 모습의 실험체가 모습을 드러냅니다." : visible.story, 14, Muted);
        }

        void DrawRareCardSelection()
        {
            var s = Engine.State;
            bool copy = s.rareCopyPending;
            if (!ReferenceEquals(rareSelectionState, s) || rareSelectionNode != s.activeNodeId || rareSelectionIsCopy != copy)
            {
                rareRemovalSelection.Clear(); rareCopySelection = -1; rareCardScroll = Vector2.zero;
                rareSelectionState = s; rareSelectionNode = s.activeNodeId; rareSelectionIsCopy = copy;
            }
            var snapshot = EnsureDeckSnapshot();
            if (!ReferenceEquals(rareCandidateState, s) || rareCandidateRevision != snapshot.Revision || rareCandidateCopy != copy)
            {
                rareCandidateIndices = copy ? Engine.RareCopyIndices() : Engine.RareRemovalIndices();
                rareCandidateState = s; rareCandidateRevision = snapshot.Revision; rareCandidateCopy = copy;
            }
            var candidates = rareCandidateIndices;
            rareRemovalSelection.RemoveWhere(i => !candidates.Contains(i));
            if (!candidates.Contains(rareCopySelection)) rareCopySelection = -1;
            int limit = Math.Min(2, Math.Max(0, s.deck.Count - 5));
            Txt(new Rect(36, 111, 1200, 32), copy ? "카드 복제 / 일반 사본 2장을 추가할 카드를 선택하세요." : "덱 정리 / 영구 제거할 카드를 최대 2장 선택하세요.", 23, RareTone);
            Para(new Rect(36, 151, 1200, 42), copy ? "미강화 카드 중 기본 코스트가 1 이하인 카드를 한 장 선택합니다. 같은 카드를 다시 누르면 취소합니다. 기존 카드는 유지됩니다."
                : "무기 스킬은 제외하며, 제거 후 덱에 최소 5장이 남아야 합니다. 같은 카드를 다시 누르면 취소합니다. 확정 전에는 제거되지 않습니다.", 13, Muted);
            Txt(new Rect(36, 195, 1200, 23), copy ? "복제 가능 " + candidates.Count + "장 · 선택 " + (rareCopySelection >= 0 ? "1 / 1" : "0 / 1") : "제거 가능 " + candidates.Count + "장 · 선택 " + rareRemovalSelection.Count + " / " + limit, 14, Gold);
            rareCardScroll = BeginTouchScrollView("rare-card-selection", new Rect(36, 231, 1202, 353), rareCardScroll, new Rect(0, 0, 1174, Math.Max(340, Mathf.Ceil(candidates.Count / 6f) * 246)));
            var visible = CraftingCatalog.VisibleItems(candidates.Count, rareCardScroll.y, 353, 6, 246);
            for (int i = visible.first; i < visible.end; i++)
            {
                int deckIndex = candidates[i]; bool selected = copy ? rareCopySelection == deckIndex : rareRemovalSelection.Contains(deckIndex);
                bool enabled = copy || selected || rareRemovalSelection.Count < limit;
                Rect r = new Rect(i % 6 * 196, i / 6 * 246, 181, 215);
                DrawCard(r, s.deck[deckIndex], selected, enabled);
                Txt(new Rect(r.x, r.yMax + 3, r.width, 20), "덱의 " + (deckIndex + 1) + "번째 카드" + (selected ? " · 선택됨" : ""), 10, selected ? Gold : Muted, true);
                if (Hit(r, enabled)) ToggleRareCardSelection(deckIndex, copy);
            }
            if (candidates.Count == 0) Para(new Rect(20, 32, 1120, 68), "선택할 수 있는 카드가 없습니다. 카드 변경 없이 다음 구역으로 진행할 수 있습니다.", 18, Muted);
            EndTouchScrollView();
            if (Btn(new Rect(36, 626, 200, 48), "선택 취소", false, Muted, rareCopySelection >= 0 || rareRemovalSelection.Count > 0)) { rareCopySelection = -1; rareRemovalSelection.Clear(); }
            if (Btn(new Rect(255, 626, 454, 48), copy ? "선택한 카드의 일반 사본 2장 추가" : "선택한 " + rareRemovalSelection.Count + "장 영구 제거", true, RareTone, copy ? rareCopySelection >= 0 : rareRemovalSelection.Count > 0))
                if (Act(() => copy ? Engine.ConfirmRareCopy(rareCopySelection) : Engine.ConfirmRareRemoval(rareRemovalSelection.ToArray()), "encounter.choose")) { rareCopySelection = -1; rareRemovalSelection.Clear(); }
            if (Btn(new Rect(960, 626, 280, 48), "카드 변경 없이 진행  >", false, Muted))
                if (Act(copy ? (Func<bool>)Engine.SkipRareCopy : Engine.SkipRareRemoval, "ui.cancel")) { rareCopySelection = -1; rareRemovalSelection.Clear(); }
        }

        void ToggleRareCardSelection(int index, bool copy)
        {
            if (copy) rareCopySelection = rareCopySelection == index ? -1 : index;
            else if (!rareRemovalSelection.Remove(index) && rareRemovalSelection.Count < Math.Min(2, Math.Max(0, Engine.State.deck.Count - 5))) rareRemovalSelection.Add(index);
        }

        public string[] RareV2VerificationViews => new[]
        {
            "rare_v2_before", "rare_v2_during", "rare_v2_after", "rare_v2_reveal_reloaded", "rare_v2_purchase_detail",
            "rare_v2_hand", "rare_v2_inventory", "rare_v2_detail", "rare_v2_card_reloaded",
            "rare_v2_remove_select", "rare_v2_remove_selected", "rare_v2_remove_reloaded", "rare_v2_remove_cancelled", "rare_v2_remove_confirmed",
            "rare_v2_copy_select", "rare_v2_copy_selected", "rare_v2_copy_reloaded", "rare_v2_copy_confirmed",
            "rare_v2_runes", "rare_v2_runes_scroll", "rare_v2_passive_swap", "rare_v2_research_500", "rare_v2_research_insufficient", "rare_v2_skip"
        };

        void PrepareRareV2Run(int seed)
        {
            if (!RareV2VerificationEnabled || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("Rare v2 UI verification requires the isolated development save.");
            lobby = catalog = inventory = settings = help = fieldInfo = enemyLoadout = research = modeSelecting = runMenu = endlessConfirm = false;
            inspectCard = inspectTrait = inspectGear = inspectBoss = mapIntel = ""; inspectEnemyCard = inspectRareReward = inspectTacticalReward = false; summaryMode = true;
            scroll = inventoryScroll = detailScroll = encounterStoryScroll = rareCardScroll = Vector2.zero; summaryScroll.Clear();
            rareSelectionState = null; rareRemovalSelection.Clear(); rareCopySelection = -1; rareRevealKey = ""; rareRevealVerificationTime = -1;
            effects.Clear(); CloseCombatInformation(); CloseStoryReplay(); CloseStoryLayers(); tutorial = null; tutorialOffer = tutorialQueued = false; toastUntil = 0;
            var profile = ResearchVerificationProfile(750, true, true);
            profile.completedReports = GameDatabase.Characters.Select(character => character.id).ToList();
            ResetResearchVerificationProfile(profile); Engine = NewResearchVerificationRun(seed);
            InitializeRunStory(); StoryScenes.Skip(Engine.State);
            var s = Engine.State; s.credits = 1000; s.passives.Clear(); s.gear.Clear(); s.weaponGrantedCards.Clear();
            s.deck = new List<string> { "basic_attack", "basic_attack", "basic_guard", "basic_guard", "nia_q", "nia_q", "nia_w", "aya_q", "jackie_q", "nia_e" };
            s.cardUpgrades.Clear(); s.upgrades.Clear(); s.combatHistory.Clear(); Engine.CombatActions.Clear();
        }

        EventDef EnterRareV2Event(string subject, string effect = null)
        {
            var ev = subject != null ? ResearchEncounters.EventForSubject(subject) : ResearchEncounters.Events.First(e => e.options.Any(o => o.effect == effect));
            var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Encounter;
            if (!Engine.EnterNode(node.lane)) throw new InvalidOperationException("Rare v2 fixture could not enter its actual encounter node.");
            Engine.State.encounterOffers = new List<string> { ev.id };
            Engine.State.rareRevealEventId = ev.id;
            Engine.State.rareRevealSourceEventId = GameDatabase.Events.First(e => !e.chainOnly && e.owner == ev.owner).id;
            Engine.State.rareRevealSeen = true;
            if (!Engine.SelectEncounter(ev.id)) throw new InvalidOperationException("Rare v2 fixture could not select its actual rare encounter.");
            return ev;
        }

        void ReloadRareV2State()
        {
            Save(); Engine = new GameEngine(JsonUtility.FromJson<RunState>(File.ReadAllText(savePath)));
            rareSelectionState = null; rareRemovalSelection.Clear(); rareCopySelection = -1;
            effects.Clear(); Engine.CombatActions.Clear();
        }

        public void VerificationRareV2View(string view)
        {
            PrepareRareV2Run(89606);
            if (view == "rare_v2_research_500" || view == "rare_v2_research_insufficient")
            {
                ResetResearchVerificationProfile(ResearchVerificationProfile(view.EndsWith("insufficient", StringComparison.Ordinal) ? 499 : 500, true, false));
                lobby = research = true; researchSubject = "nia"; researchReadReport = false; researchQuery = ""; researchListScroll = Vector2.zero;
                if (ResearchProgression.ReportCost != 500) throw new InvalidOperationException("Rare v2 research must cost 500 data.");
                return;
            }
            if (view == "rare_v2_before" || view == "rare_v2_during" || view == "rare_v2_after" || view == "rare_v2_reveal_reloaded")
            {
                var ev = EnterRareV2Event("nia"); var s = Engine.State; s.chosenEventId = null;
                s.encounterOffers = new List<string> { ev.id, GameDatabase.Events.First(e => !e.chainOnly && e.owner == "아야").id, GameDatabase.Events.First(e => !e.chainOnly && e.owner == "아이솔").id };
                s.rareRevealSeen = view == "rare_v2_after" || view == "rare_v2_reveal_reloaded";
                rareRevealVerificationTime = view == "rare_v2_before" ? 0 : .95f;
                if (view == "rare_v2_reveal_reloaded")
                {
                    ReloadRareV2State();
                    if (!Engine.State.rareRevealSeen || UpdateRareReveal()) throw new InvalidOperationException("An already viewed rare reveal must remain complete after reload.");
                }
                else if (!s.rareRevealSeen && !UpdateRareReveal()) throw new InvalidOperationException("The rare reveal must block encounter choices before it finishes.");
                return;
            }
            if (view.StartsWith("rare_v2_remove", StringComparison.Ordinal) || view.StartsWith("rare_v2_copy", StringComparison.Ordinal))
            {
                bool copy = view.StartsWith("rare_v2_copy", StringComparison.Ordinal);
                var ev = EnterRareV2Event(null, copy ? "rare_copy_cards" : "rare_remove_cards");
                string rareRef = Engine.AddRareRewardCard("nia_q"); int before = Engine.State.deck.Count;
                int option = Array.FindIndex(ev.options, o => o.effect == (copy ? "rare_copy_cards" : "rare_remove_cards"));
                if (!Engine.ChooseEventOption(option) || !(copy ? Engine.State.rareCopyPending : Engine.State.rareRemovalPending))
                    throw new InvalidOperationException("Rare card selection must open only after choosing the actual authored option.");
                if (view.EndsWith("reloaded", StringComparison.Ordinal))
                {
                    ReloadRareV2State();
                    if (!(copy ? Engine.State.rareCopyPending : Engine.State.rareRemovalPending) || Engine.CardUpgradeLevel(rareRef) != 2)
                        throw new InvalidOperationException("Pending rare card selection and the individual ++ card must survive save reload.");
                }
                rareSelectionState = Engine.State; rareSelectionNode = Engine.State.activeNodeId; rareSelectionIsCopy = copy;
                if (view.EndsWith("select", StringComparison.Ordinal)) return;
                int index = copy ? Engine.RareCopyIndices().First() : Engine.State.deck.IndexOf(rareRef);
                ToggleRareCardSelection(index, copy);
                if (!copy) ToggleRareCardSelection(4, false);
                if (view.EndsWith("cancelled", StringComparison.Ordinal))
                {
                    ToggleRareCardSelection(index, false); ToggleRareCardSelection(4, false);
                    if (rareRemovalSelection.Count != 0 || Engine.State.deck.Count != before) throw new InvalidOperationException("Cancelling rare removal must keep every card.");
                }
                if (view.EndsWith("confirmed", StringComparison.Ordinal))
                {
                    string ordinary = Engine.State.deck[index]; int originalCopies = Engine.State.deck.Count(id => id == ordinary);
                    bool result = copy ? Engine.ConfirmRareCopy(index) : Engine.ConfirmRareRemoval(rareRemovalSelection.ToArray());
                    if (!result || Engine.State.deck.Count != before + (copy ? 2 : -2) || Engine.State.stage != RunStage.Map
                        || copy && (Engine.State.deck.Count(id => id == ordinary) != originalCopies + 2 || Engine.State.deck.Skip(Engine.State.deck.Count - 2).Any(id => Engine.CardUpgradeLevel(id) != 0)))
                        throw new InvalidOperationException("Rare confirmation must apply exactly the chosen card changes and complete the encounter once.");
                    ReloadRareV2State(); inventory = true; inventoryTab = 0;
                }
                return;
            }
            if (view == "rare_v2_runes" || view == "rare_v2_runes_scroll")
            {
                var ev = EnterRareV2Event(null, "rare_support_runes");
                int option = Array.FindIndex(ev.options, o => o.effect == "rare_support_runes");
                if (!Engine.ChooseEventOption(option)) throw new InvalidOperationException("Rare support rune option was not applied.");
                ReloadRareV2State(); inventory = true; inventoryTab = 4;
                if (Engine.OwnedRuneIds().Count < 3 || Engine.OwnedRuneIds().Distinct().Count() != Engine.OwnedRuneIds().Count)
                    throw new InvalidOperationException("The rune inventory must contain every additional distinct support rune after reload.");
                if (view.EndsWith("scroll", StringComparison.Ordinal)) inventoryScroll = new Vector2(0, 10000);
                return;
            }
            if (view == "rare_v2_passive_swap")
            {
                var ev = EnterRareV2Event("nia");
                Engine.State.passives = GameDatabase.Passives.Where(p => p.id != "nia_p").Take(3).Select(p => p.id).ToList();
                if (!Engine.ChooseEventOption(Array.FindIndex(ev.options, o => o.effect == "passive")) || Engine.State.stage != RunStage.PassiveChoice || Engine.State.pendingPassive != "nia_p")
                    throw new InvalidOperationException("Rare passive rewards must use the existing three-slot replacement screen.");
                return;
            }
            var encounter = EnterRareV2Event("nia"); var purchase = encounter.options.First(o => o.effect == "rare_trade_card");
            if (view == "rare_v2_purchase_detail")
            {
                inspectCard = purchase.cardId; inspectRareReward = true; inspectTacticalReward = false; summaryMode = false;
                var preview = EventPresentation.RewardCard(purchase); var actual = RareCardUpgrades.Apply(GameDatabase.Card(purchase.cardId));
                if (preview.damage != actual.damage || preview.cost != actual.cost || preview.damage <= GameDatabase.Card(purchase.cardId).damage)
                    throw new InvalidOperationException("Purchase preview must show the exact ++ card values before the card enters the deck.");
                return;
            }
            if (view == "rare_v2_skip")
            {
                Engine.State.credits = 0; Engine.State.passives.Add("nia_p");
                Engine.State.additionalSupportRunes = GameDatabase.Runes.Where(r => !r.main && r.tree == GameDatabase.Rune(Engine.State.mainRune).tree).Select(r=>r.id).ToList();
                if (encounter.options.Select((o,i)=>Engine.CanChooseEventOption(i)).Any(available=>available))
                    throw new InvalidOperationException("The rare skip screen must exercise an encounter with no available rewards.");
                return;
            }
            int purchaseIndex = Array.IndexOf(encounter.options, purchase); int originalCount = Engine.State.deck.Count;
            if (!Engine.ChooseEventOption(purchaseIndex) || Engine.State.deck.Count != originalCount + 1) throw new InvalidOperationException("Rare purchase must add exactly one card.");
            string upgraded = Engine.State.deck.Last(); var upgradedCard = Engine.PlayerCard(upgraded);
            Engine.State.deck.Insert(0, purchase.cardId); Engine.State.deck.Insert(0, purchase.cardId);
            if (Engine.CardUpgradeSuffix(upgraded) != " ++" || Engine.CardUpgradeLevel(upgraded) != 2 || Engine.CardUpgradeSuffix(purchase.cardId) != "")
                throw new InvalidOperationException("Only the purchased individual copy may carry the ++ suffix.");
            if (view == "rare_v2_card_reloaded")
            {
                ReloadRareV2State();
                if (Engine.CardUpgradeLevel(upgraded) != 2 || Engine.PlayerCard(upgraded).damage != upgradedCard.damage || Engine.CardUpgradeLevel(purchase.cardId) != 0)
                    throw new InvalidOperationException("Rare ++ card effects and ordinary duplicates must persist independently.");
            }
            if (view == "rare_v2_inventory") { inventory = true; inventoryTab = 0; return; }
            if (view == "rare_v2_detail" || view == "rare_v2_card_reloaded") { inspectCard = upgraded; summaryMode = false; return; }
            var battle = Engine.AvailableNodes().First(); battle.kind = ZoneKind.Subject;
            if (!Engine.EnterNode(battle.lane)) throw new InvalidOperationException("Rare ++ hand fixture could not enter actual combat.");
            var combat = Engine.State.combat; combat.hand = new List<string> { purchase.cardId, upgraded, purchase.cardId, "basic_attack", "basic_guard" };
            combat.drawPile.Clear(); combat.discardPile.Clear(); combat.energy = 5; combat.enemyHp = combat.enemyMaxHp = 9999; combat.enemyPlan.Clear();
            displayedTurn = combat.turn; effects.Clear(); Engine.CombatActions.Clear();
        }
    }
}
