#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void ChooseRuneTree(GameEngine engine, string tree)
    {
        var main = GameDatabase.Runes.First(rune => rune.main && rune.tree == tree);
        var support = GameDatabase.Runes.First(rune => !rune.main && rune.tree == tree);
        Check(engine.SetRunes(main.id, support.id), "rune role fixture selects " + tree);
    }

    static List<string> SamplePassives(GameEngine engine, IEnumerable<string> pool, int amount)
    {
        object[] args = { pool, amount, engine.State.rngState };
        var result = (List<string>)typeof(GameEngine).GetMethod("PassiveOffer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(engine, args);
        engine.State.rngState = (int)args[2]; return result;
    }

    static void RuneRoleCoverage()
    {
        Check(GameDatabase.Characters.Count == 91 && GameDatabase.Characters.All(character => SubjectRoleCatalog.ForSubject(character.id) != SubjectRole.None), "all 91 game subjects have an official role");
        Check(SubjectRoleCatalog.ForSubject("lumi") == SubjectRole.None && SubjectRoleCatalog.ForSubject("wickeline") == SubjectRole.None && SubjectRoleCatalog.ForSubject(null) == SubjectRole.None, "NPCs and invalid subjects have no role");
        var roles = new[] { SubjectRole.Fighter, SubjectRole.Marksman, SubjectRole.Tank, SubjectRole.Mage, SubjectRole.Assassin, SubjectRole.Support };
        var expected = new[] { 38, 16, 12, 31, 4, 9 };
        for (int i = 0; i < roles.Length; i++) Check(GameDatabase.Characters.Count(character => (SubjectRoleCatalog.ForSubject(character.id) & roles[i]) != 0) == expected[i], "official role count " + roles[i]);
        Check(SubjectRoleCatalog.ForSubject("nia") == SubjectRole.Mage && SubjectRoleCatalog.ForSubject("aya") == (SubjectRole.Mage | SubjectRole.Marksman)
            && SubjectRoleCatalog.ForSubject("mai") == (SubjectRole.Tank | SubjectRole.Support), "official single and dual roles are preserved rather than inferred from a weapon");
        var engine = new GameEngine(4412);
        foreach (string tree in new[] { "파괴", "저항", "혼돈", "지원" })
        {
            ChooseRuneTree(engine, tree);
            foreach (var character in GameDatabase.Characters)
            {
                bool favored = (SubjectRoleCatalog.ForSubject(character.id) & SubjectRoleCatalog.FavoredRoles(tree)) != 0;
                Check(character.cards.All(id => engine.IsRuneFavoredCard(id) == favored) && engine.IsRuneFavoredPassive(character.passiveId) == favored, "all QWER and passive share official rune role affinity: " + tree + "/" + character.id);
            }
            Check(new[] { "basic_attack", "weapon_glove", "tactical_blink", "missing", null }.All(id => !engine.IsRuneFavoredCard(id)), "non-subject cards never receive role affinity");
            Check(SubjectRoleCatalog.OfferDetails(tree).Contains("4배") && SubjectRoleCatalog.OfferDetails(tree).Contains("7.2배") && SubjectRoleCatalog.OfferDetails(tree).Contains("고정 보상"), "rune details disclose weight, passive combination and fixed reward exclusions");
        }
    }

    static void RuneWeightedDistributions()
    {
        var engine = new GameEngine(6617); ChooseRuneTree(engine, "혼돈"); engine.State.passives.Clear();
        string ordinary = GameDatabase.Characters.First(character => !engine.IsRuneFavoredCard(character.cards[0])).cards[0];
        var pair = new[] { "nia_q", ordinary };
        var plain = new GameEngine((RunState)Clone(engine.State)); plain.State.mainRune = null;
        var combined = new GameEngine((RunState)Clone(engine.State)); combined.State.passives.Add("nia_p");
        int plainHits = 0, roleHits = 0, combinedHits = 0, passiveHits = 0;
        const int samples = 16000;
        for (int i = 0; i < samples; ++i)
        {
            if (SampleCards(plain, pair, 1)[0] == "nia_q") plainHits++;
            if (SampleCards(engine, pair, 1)[0] == "nia_q") roleHits++;
            if (SampleCards(combined, pair, 1)[0] == "nia_q") combinedHits++;
            if (SamplePassives(engine, new[] { "nia_p", GameDatabase.Characters.First(character => character.cards.Contains(ordinary)).passiveId }, 1)[0] == "nia_p") passiveHits++;
        }
        Check(Math.Abs((double)plainHits / samples - .5) < .02, "unweighted pair remains 50/50");
        Check(Math.Abs((double)roleHits / samples - .8) < .02 && Math.Abs((double)passiveHits / samples - .8) < .02, "fourfold card and passive weights produce an 80/20 candidate pair");
        Check(Math.Abs((double)combinedHits / samples - 7.2 / 8.2) < .02, "owned passive preference multiplies the role weight to 7.2 rather than replacing or adding it");
        Console.WriteLine("Rune pair distribution: plain=" + plainHits + "/" + samples + ", role=" + roleHits + "/" + samples + ", passive=" + passiveHits + "/" + samples + ", combined=" + combinedHits + "/" + samples + ".");
        var dual = new GameEngine(6619); ChooseRuneTree(dual, "지원"); dual.State.passives.Clear();
        string other = GameDatabase.Characters.First(character => !dual.IsRuneFavoredPassive(character.passiveId)).passiveId;
        int dualHits = 0;
        for (int i = 0; i < samples; ++i) if (SamplePassives(dual, new[] { "mai_p", other }, 1)[0] == "mai_p") dualHits++;
        Check(Math.Abs((double)dualHits / samples - .8) < .02, "matching both green roles still weighs fourfold, not sixteenfold");

        foreach (string tree in new[] { "파괴", "저항", "혼돈", "지원" })
        {
            int basePassives = 0, rolePassives = 0, baseCards = 0, roleCards = 0;
            const int runs = 160;
            for (int seed = 2101; seed < 2101 + runs; ++seed)
            {
                var run = new GameEngine(seed);
                var originalPassives = run.State.passiveOffers.ToArray(); var originalCards = run.State.draftOffers.ToArray();
                ChooseRuneTree(run, tree);
                basePassives += originalPassives.Count(run.IsRuneFavoredPassive); baseCards += originalCards.Count(run.IsRuneFavoredCard);
                rolePassives += run.State.passiveOffers.Count(run.IsRuneFavoredPassive); roleCards += run.State.draftOffers.Count(run.IsRuneFavoredCard);
                Check(run.State.passiveOffers.Distinct().Count() == 3 && run.State.draftOffers.Distinct().Count() == 6, "real preparation samples without replacement");
            }
            Check(rolePassives > basePassives + runs * 3 * .15 && roleCards > baseCards + runs * 6 * .15, "visible initial passive/card offers gain a substantial role preference: " + tree);
            Console.WriteLine("Rune preparation distribution: " + tree + ", passive " + basePassives + "->" + rolePassives + "/" + runs * 3 + ", card " + baseCards + "->" + roleCards + "/" + runs * 6 + ".");
        }
        var duplicate = new GameEngine((RunState)Clone(engine.State));
        duplicate.State.additionalSupportRunes.AddRange(GameDatabase.Runes.Where(rune => !rune.main).Select(rune => rune.id));
        Check(SampleCards(engine, pair, 30).SequenceEqual(SampleCards(duplicate, pair.Concat(pair), 30)) && engine.State.rngState == duplicate.State.rngState, "extra support runes and duplicate pool entries do not stack role weight or duplicate offers");
        int before = engine.State.rngState;
        Check(SamplePassives(engine, new string[0], 3).Count == 0 && SampleCards(engine, pair, 0).Count == 0 && engine.State.rngState == before, "empty and zero-size weighted samples consume no RNG");
    }

    static void RuneStartingChoicesAndSaves()
    {
        var engine = new GameEngine(9971); ChooseRuneTree(engine, "파괴");
        Check(engine.SelectStartingPassive(engine.State.passiveOffers[1]), "choose a starting passive before changing rune color");
        engine.ToggleDraft(engine.State.draftOffers[0]); engine.ToggleDraft(engine.State.draftOffers[3]);
        var passives = engine.State.passiveOffers.ToArray(); var cards = engine.State.draftOffers.ToArray();
        var selected = engine.State.draftSelected.ToArray(); string chosen = engine.State.chosenPassive;
        int rng = engine.State.rngState, draftSeed = engine.State.draftSeed, passiveSeed = engine.State.passiveOfferSeed;
        ChooseRuneTree(engine, "지원");
        Check(engine.State.chosenPassive == chosen && engine.State.passiveOffers[1] == chosen && engine.State.draftOffers[0] == cards[0] && engine.State.draftOffers[3] == cards[3]
            && engine.State.draftSelected.SequenceEqual(selected), "rune changes retain selected passive and physical card slots");
        ChooseRuneTree(engine, "파괴");
        Check(engine.State.passiveOffers.SequenceEqual(passives) && engine.State.draftOffers.SequenceEqual(cards) && engine.State.rngState == rng
            && engine.State.draftSeed == draftSeed && engine.State.passiveOfferSeed == passiveSeed, "switching rune colors away and back cannot farm new random choices");
        var loaded = new GameEngine((RunState)Clone(engine.State));
        Check(loaded.State.passiveOffers.SequenceEqual(passives) && loaded.State.draftOffers.SequenceEqual(cards) && loaded.State.rngState == rng, "reload keeps the exact displayed weighted choices");
        Check(engine.RerollDraft() && loaded.RerollDraft() && engine.State.draftOffers.SequenceEqual(loaded.State.draftOffers) && engine.State.draftSelected.SequenceEqual(selected), "paid card reroll remains reproducible and preserves selected cards");
        Check(engine.RerollPassives() && loaded.RerollPassives() && engine.State.passiveOffers.SequenceEqual(loaded.State.passiveOffers)
            && engine.State.draftOffers.SequenceEqual(loaded.State.draftOffers) && engine.State.passiveOfferSeed != passiveSeed, "paid passive reroll uses current rune roles and is deterministic after reload");

        foreach (bool oldDraftSeed in new[] { false, true })
        {
            var legacy = new GameEngine(9982); ChooseRuneTree(legacy, "파괴");
            legacy.SelectStartingPassive(legacy.State.passiveOffers[0]); legacy.ToggleDraft(legacy.State.draftOffers[0]);
            legacy.State.passiveOfferSeed = 0; legacy.State.passiveBiasRuneTree = legacy.State.draftBiasRuneTree = null;
            if (oldDraftSeed) legacy.State.draftSeed = 0;
            var oldPassives = legacy.State.passiveOffers.ToArray(); var oldCards = legacy.State.draftOffers.ToArray();
            rng = legacy.State.rngState; loaded = new GameEngine((RunState)Clone(legacy.State));
            loaded.SelectStartingPassive(loaded.State.chosenPassive);
            Check(loaded.State.passiveOffers.SequenceEqual(oldPassives) && loaded.State.draftOffers.SequenceEqual(oldCards) && loaded.State.rngState == rng, "legacy preparation reload/reselect never redraws existing offers");
            ChooseRuneTree(loaded, "혼돈");
            Check(loaded.State.passiveOfferSeed != 0 && loaded.State.draftSeed != 0 && loaded.State.draftBiasRuneTree == "혼돈"
                && loaded.State.passiveOffers.Contains(loaded.State.chosenPassive) && loaded.State.draftOffers.Contains(oldCards[0]), "explicit legacy rune change opts in while preserving selected choices");
        }
    }

    static void RuneRewardBoundaries()
    {
        var engine = Ready(70031); Enter(engine, ZoneKind.Wildlife); engine.State.mainRune = "healing_drone"; engine.State.passives.Clear();
        var loaded = new GameEngine((RunState)Clone(engine.State));
        var predictor = new GameEngine((RunState)Clone(engine.State));
        typeof(GameEngine).GetMethod("Next", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null).Invoke(predictor, new object[] { 25 });
        var expected = SampleCards(predictor, GameDatabase.Cards.Where(card => card.category == "skill" || card.category == "tactical").Select(card => card.id), 3);
        typeof(GameEngine).GetMethod("WinBattle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(engine, null);
        typeof(GameEngine).GetMethod("WinBattle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(loaded, null);
        Check(engine.State.rewards.choices.Where(id => id != "basic_attack").SequenceEqual(expected) && engine.State.rewards.choices.SequenceEqual(loaded.State.rewards.choices), "actual wildlife reward and combat save use the current rune's weighted sampler");
        foreach (var kind in new[] { ZoneKind.Subject, ZoneKind.Boss })
        {
            engine = Ready(70032); if (kind == ZoneKind.Boss) engine.State.row = 10; Enter(engine, kind);
            loaded = new GameEngine((RunState)Clone(engine.State)); engine.State.passives.Clear(); loaded.State.passives.Clear();
            engine.State.mainRune = "vampire"; loaded.State.mainRune = "lightning";
            string opponent = engine.State.combat.enemyId;
            var fixedCards = engine.State.combat.enemyDeck.Distinct().Where(id => GameDatabase.Card(id).category == "skill").ToArray();
            typeof(GameEngine).GetMethod("WinBattle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(engine, null);
            typeof(GameEngine).GetMethod("WinBattle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(loaded, null);
            Check(engine.State.rewards.choices.SequenceEqual(fixedCards) && loaded.State.rewards.choices.SequenceEqual(fixedCards), "runes never replace the defeated subject's authored skill rewards");
            if (kind == ZoneKind.Boss) Check(engine.State.pendingPassive == GameDatabase.Character(opponent).passiveId && loaded.State.pendingPassive == engine.State.pendingPassive, "boss passive stays guaranteed to its defeated subject");
        }
        engine = Ready(70033); loaded = new GameEngine((RunState)Clone(engine.State)); engine.State.passives.Clear(); loaded.State.passives.Clear();
        engine.State.mainRune = "vampire"; loaded.State.mainRune = "lightning";
        Enter(engine, ZoneKind.Wildlife); Enter(loaded, ZoneKind.Wildlife);
        Check(engine.State.combat.hand.SequenceEqual(loaded.State.combat.hand) && engine.State.rngState == loaded.State.rngState, "role bias never changes combat hand shuffling");
    }
}
#endif
