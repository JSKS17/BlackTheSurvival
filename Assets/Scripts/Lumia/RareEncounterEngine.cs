using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public int GlobalAct => State.mode == RunMode.Endless ? (int)Math.Min(int.MaxValue, (long)Math.Max(0, State.endlessCycle) * 3 + Math.Max(1, State.act)) : Math.Max(1, State.act);
        public static int RareOfferChanceForAct(int globalAct) => globalAct <= 1 ? 12 : globalAct == 2 ? 18 : 25;
        public int CurrentRareOfferChance => RareOfferChanceForAct(GlobalAct);

        public List<string> OwnedRuneIds()
        {
            return new[] { State.mainRune, State.supportRune }.Concat(State.additionalSupportRunes ?? new List<string>())
                .Where(id => !string.IsNullOrEmpty(id) && GameDatabase.Rune(id) != null).Distinct().ToList();
        }

        List<string> AvailableRareSupportRunes()
        {
            var main = GameDatabase.Rune(State.mainRune);
            if (main == null || !main.main) return new List<string>();
            var owned = new HashSet<string>(OwnedRuneIds());
            return GameDatabase.Runes.Where(rune => !rune.main && rune.tree == main.tree && !owned.Contains(rune.id)).Select(rune => rune.id).ToList();
        }

        void NormalizeRareEncounters()
        {
            State.additionalSupportRunes = (State.additionalSupportRunes ?? new List<string>())
                .Where(id => GameDatabase.Rune(id) != null && !GameDatabase.Rune(id).main).Distinct().ToList();
            State.rareOfferedActs = (State.rareOfferedActs ?? new List<int>()).Where(act => act > 0).Distinct().ToList();
            State.encounterOffers = State.encounterOffers ?? new List<string>();
            if (State.rareEncounterVersion < 2)
            {
                var shown = State.encounterOffers.Where(id => ResearchEncounters.SubjectForEvent(id) != null).ToList();
                if (shown.Count > 0 || ResearchEncounters.SubjectForEvent(State.chosenEventId) != null)
                {
                    if (!State.rareOfferedActs.Contains(GlobalAct)) State.rareOfferedActs.Add(GlobalAct);
                    string keep = shown.Contains(State.chosenEventId) ? State.chosenEventId : shown.FirstOrDefault();
                    foreach (string id in shown.Where(id => id != keep).ToArray())
                    {
                        int index = State.encounterOffers.IndexOf(id);
                        var ordinary = OrdinaryForRare(id);
                        if (ordinary != null) State.encounterOffers[index] = ordinary.id;
                    }
                    State.rareRevealEventId = keep;
                    State.rareRevealSourceEventId = OrdinaryForRare(keep)?.id;
                    // These candidates were already visible before migration.
                    State.rareRevealSeen = true;
                }
                State.rareEncounterVersion = 2;
            }
            bool isRare = ResearchEncounters.SubjectForEvent(State.chosenEventId) != null;
            if (State.stage != RunStage.Encounter || !isRare)
            {
                State.rareRemovalPending = State.rareCopyPending = false;
            }
            else
            {
                // New saves have one pending action. Corrupt overlapping flags
                // cannot open two independent claims on the same encounter.
                if (State.runeChangePending || State.cardUpgradePending) State.rareRemovalPending = State.rareCopyPending = false;
                else if (State.rareRemovalPending) State.rareCopyPending = false;
                var ev = GameDatabase.Event(State.chosenEventId);
                if (State.rareRemovalPending && !ev.options.Any(o => o.effect == "rare_remove_cards")) State.rareRemovalPending = false;
                if (State.rareCopyPending && !ev.options.Any(o => o.effect == "rare_copy_cards")) State.rareCopyPending = false;
            }
            if (State.stage != RunStage.Encounter)
            {
                State.rareRevealEventId = State.rareRevealSourceEventId = null; State.rareRevealSeen = false;
            }
        }

        static EventDef OrdinaryForRare(string id)
        {
            var rare = ResearchEncounters.Event(id);
            return rare == null ? null : GameDatabase.Events.FirstOrDefault(ev => !ev.chainOnly && ev.owner == rare.owner);
        }

        void ClearRarePending()
        {
            State.rareRemovalPending = State.rareCopyPending = false;
            State.rareRevealEventId = State.rareRevealSourceEventId = null; State.rareRevealSeen = false;
        }

        void RollRareEncounterOffers()
        {
            State.pendingEncounterFollowups.RemoveAll(x => x == null || GameDatabase.Event(x.eventId) == null || State.completedChainEvents.Contains(x.eventId));
            var next = NextPromisedEncounter();
            var pendingOwners = new HashSet<string>(State.pendingEncounterFollowups.Select(x => x.owner));
            var ordinary = GameDatabase.Events.Where(ev => !ev.chainOnly && !pendingOwners.Contains(ev.owner)).ToList();
            var originalIds = new Dictionary<string, string>();
            if (!State.rareOfferedActs.Contains(GlobalAct))
            {
                for (int i = 0; i < ordinary.Count; i++)
                {
                    var subject = GameDatabase.Characters.FirstOrDefault(character => character.name == ordinary[i].owner);
                    var rare = ResearchEncounters.EventForSubject(subject?.id);
                    if (rare == null || !State.research.unlockedReports.Contains(subject.id)
                        || State.research.completedResearchEvents.Contains(rare.id) || !Roll(CurrentRareOfferChance)) continue;
                    originalIds[rare.id] = ordinary[i].id; ordinary[i] = rare;
                }
            }
            ordinary.AddRange(State.pendingEncounterFollowups.Where(x => x != next).Select(x => GameDatabase.Event(x.eventId)).Where(x => x != null));
            bool featured = next != null && Roll(FollowupOfferChance);
            State.encounterOffers = Offer(ordinary.Select(ev => ev.id), featured ? 2 : 3);
            if (featured) { State.encounterOffers.Add(next.eventId); Shuffle(State.encounterOffers); }
            var rares = State.encounterOffers.Where(id => originalIds.ContainsKey(id)).ToList();
            if (rares.Count > 0)
            {
                string keep = rares[Next(rares.Count)];
                foreach (string id in rares.Where(id => id != keep)) State.encounterOffers[State.encounterOffers.IndexOf(id)] = originalIds[id];
                State.rareOfferedActs.Add(GlobalAct);
                State.rareRevealEventId = keep; State.rareRevealSourceEventId = originalIds[keep]; State.rareRevealSeen = false;
            }
            else { State.rareRevealEventId = State.rareRevealSourceEventId = null; State.rareRevealSeen = false; }
            if (next != null)
            {
                next.priorityNext = false;
                State.pendingEncounterFollowups.Remove(next); State.pendingEncounterFollowups.Add(next);
            }
        }

        bool CanChooseRareOption(EventOption option) => string.IsNullOrEmpty(RareOptionUnavailableReason(option));
        string RareOptionUnavailableReason(EventOption option)
        {
            if (option == null) return "선택할 수 없는 행동입니다.";
            switch (option.effect)
            {
                case "rare_trade_card":
                    if (GameDatabase.Card(option.cardId) == null) return "구입할 기술이 없습니다.";
                    if (State.credits < Math.Max(0, option.amount) + Math.Max(0, option.creditCost)) return "필요한 크레딧이 부족합니다.";
                    break;
                case "rare_support_runes": if (AvailableRareSupportRunes().Count == 0) return "현재 계열의 보조 룬을 이미 모두 보유했거나 메인 룬이 없습니다."; break;
                case "rare_copy_cards": if (RareCopyIndices().Count == 0) return "복제할 수 있는 미강화·원본 코스트 1 이하 카드가 없습니다."; break;
                case "rare_tactical": if (!GameDatabase.Cards.Any(card => card.category == "tactical")) return "획득할 전술 카드가 없습니다."; break;
                case "passive":
                    if (ResearchEncounters.SubjectForEvent(State.chosenEventId) != null && (GameDatabase.Passive(option.passiveId) == null || State.passives.Contains(option.passiveId))) return "이미 보유한 패시브입니다.";
                    break;
            }
            return "";
        }

        public List<int> RareRemovalIndices() => Enumerable.Range(0, State.deck.Count)
            .Where(index => GameDatabase.Card(State.deck[index]) != null && GameDatabase.Card(State.deck[index]).category != "weapon").ToList();
        public bool ConfirmRareRemoval(IEnumerable<int> indices)
        {
            if (!RareActionPending(true)) return false;
            var selected = (indices ?? Enumerable.Empty<int>()).ToList();
            var eligible = new HashSet<int>(RareRemovalIndices());
            if (selected.Count > 2 || selected.Distinct().Count() != selected.Count || selected.Any(index => !eligible.Contains(index)) || (selected.Count > 0 && State.deck.Count - selected.Count < 5))
                return Fail("무기 카드 이외의 카드를 최대 2장 선택하고 덱에 최소 5장을 남겨야 합니다.");
            foreach (int index in selected.OrderByDescending(index => index)) State.deck.RemoveAt(index);
            State.rareRemovalPending = false; Say("선택한 카드 " + selected.Count + "장을 제거했습니다."); CompleteNode(); return true;
        }
        public bool SkipRareRemoval() => ConfirmRareRemoval(new int[0]);

        public List<int> RareCopyIndices() => Enumerable.Range(0, State.deck.Count).Where(index =>
            CardUpgradeLevel(State.deck[index]) == 0 && GameDatabase.Card(State.deck[index]) != null && GameDatabase.Card(State.deck[index]).cost <= 1).ToList();
        public bool ConfirmRareCopy(int deckIndex)
        {
            if (!RareActionPending(false) || !RareCopyIndices().Contains(deckIndex)) return false;
            string id = CardInstances.BaseId(State.deck[deckIndex]); State.deck.Add(id); State.deck.Add(id);
            State.rareCopyPending = false; Say(GameDatabase.Card(id).name + " 카드 2장을 복제했습니다."); CompleteNode(); return true;
        }
        public bool SkipRareCopy()
        {
            if (!RareActionPending(false)) return false;
            State.rareCopyPending = false; Say("이번 카드 복제를 포기했습니다."); CompleteNode(); return true;
        }
        bool RareActionPending(bool removal)
        {
            return State.stage == RunStage.Encounter && ResearchEncounters.SubjectForEvent(State.chosenEventId) != null
                && !State.runeChangePending && !State.cardUpgradePending && (removal ? State.rareRemovalPending && !State.rareCopyPending : State.rareCopyPending && !State.rareRemovalPending);
        }
        public bool SkipRareEncounter()
        {
            string id = State.chosenEventId;
            if (State.stage != RunStage.Encounter || ResearchEncounters.SubjectForEvent(id) == null || State.runeChangePending || State.cardUpgradePending || State.rareRemovalPending || State.rareCopyPending) return false;
            if (!State.research.completedResearchEvents.Contains(id)) State.research.completedResearchEvents.Add(id);
            Say("보상을 받지 않고 레어 조우를 떠났습니다."); CompleteNode(); return true;
        }
    }
}
