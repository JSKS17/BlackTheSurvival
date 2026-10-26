using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        string CurrentRuneTree => GameDatabase.Rune(State.mainRune)?.tree ?? "";

        public bool IsRuneFavoredCard(string cardId)
        {
            var card = GameDatabase.Card(cardId);
            return IsSubjectSkill(card) && SubjectRoleCatalog.FavorsOwner(CurrentRuneTree, card.owner);
        }

        public bool IsRuneFavoredPassive(string passiveId)
        {
            var passive = GameDatabase.Passive(passiveId);
            return passive != null && SubjectRoleCatalog.FavorsOwner(CurrentRuneTree, passive.owner);
        }

        static bool IsSubjectSkill(CardDef card) => card != null && card.category == "skill"
            && (card.key == "Q" || card.key == "W" || card.key == "E" || card.key == "R");

        int CardOfferWeight(CardDef card, HashSet<string> owners, bool runeAffinity)
        {
            int weight = FavoredSkill(card, owners) ? PassiveSkillOfferWeight : OrdinaryCardOfferWeight;
            if (runeAffinity && IsSubjectSkill(card) && SubjectRoleCatalog.FavorsOwner(CurrentRuneTree, card.owner))
                weight *= SubjectRoleCatalog.RuneOfferMultiplier;
            return weight;
        }

        List<string> WeightedOffer(IEnumerable<string> pool, int amount, Func<string, int> weight, ref int randomState)
        {
            var candidates = pool.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var weights = candidates.Select(id => Math.Max(1, weight(id))).ToList();
            var result = new List<string>();
            while (result.Count < amount && candidates.Count > 0)
            {
                int pick = Next(ref randomState, weights.Sum()), selected = 0;
                for (; selected < candidates.Count - 1; ++selected)
                {
                    pick -= weights[selected];
                    if (pick < 0) break;
                }
                result.Add(candidates[selected]); candidates.RemoveAt(selected); weights.RemoveAt(selected);
            }
            return result;
        }

        List<string> PassiveOffer(IEnumerable<string> pool, int amount, ref int randomState)
        {
            var candidates = pool.Where(id => !string.IsNullOrEmpty(id)).Distinct().Select(GameDatabase.Passive).Where(passive => passive != null).ToList();
            var weights = candidates.ToDictionary(passive => passive.id, passive => SubjectRoleCatalog.FavorsOwner(CurrentRuneTree, passive.owner)
                ? SubjectRoleCatalog.RuneOfferMultiplier : 1);
            return WeightedOffer(candidates.Select(passive => passive.id), amount, id => weights[id], ref randomState);
        }

        List<string> StartingPassiveOffers(ref int randomState)
        {
            string locked = GameDatabase.Passive(State.chosenPassive) != null ? State.chosenPassive : null;
            var fresh = PassiveOffer(GameDatabase.Passives.Select(passive => passive.id), 3, ref randomState)
                .Where(id => id != locked).Take(locked == null ? 3 : 2).ToList();
            if (locked == null) return fresh;
            int position = Math.Max(0, (State.passiveOffers ?? new List<string>()).IndexOf(locked));
            fresh.Insert(Math.Min(position, fresh.Count), locked);
            return fresh;
        }

        void RollStartingPassives()
        {
            State.passiveOfferSeed = Next(int.MaxValue) + 1;
            int randomState = State.passiveOfferSeed;
            State.passiveOffers = StartingPassiveOffers(ref randomState);
            State.passiveBiasRuneTree = CurrentRuneTree;
        }

        void RefreshStartingRuneOffers()
        {
            // Explicit rune color changes opt legacy preparation saves into the new
            // weighting. Loading an already displayed offer never calls this path.
            if (State.passiveOfferSeed == 0) State.passiveOfferSeed = Next(int.MaxValue) + 1;
            int randomState = State.passiveOfferSeed;
            State.passiveOffers = StartingPassiveOffers(ref randomState);
            State.passiveBiasRuneTree = CurrentRuneTree;
            RefreshStartingDraft(true);
        }
    }
}
