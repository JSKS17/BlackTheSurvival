#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void CombatVisualProfileCoverage()
    {
        var motifs = new HashSet<string>(("slash thrust barrage bullet sniper arrow bomb flame lightning ice arcade orb sigil trap flower water wind earth blood poison music star puppet shield heal chain prism camera chess time paper blade_orbit").Split(' '));
        var motions = new HashSet<string>(("none strike cast dash retreat leap blink orbit").Split(' '));
        var displacement = new HashSet<string>(("dash retreat leap blink orbit").Split(' '));
        var seenMotifs = new HashSet<string>();
        var seenMotions = new HashSet<string>();
        var seenColors = new HashSet<string>();
        var original = GameDatabase.Cards.ToDictionary(card => card.id, VisualCardSnapshot);
        var engine = Ready(324889);
        int random = engine.State.rngState;
        int draftRandom = engine.State.draftSeed;
        int deck = engine.State.deck.Count;

        Check(GameDatabase.Cards.Count == 392, "visual coverage fixture includes the entire 392-card runtime catalog");
        foreach (var card in GameDatabase.Cards)
        {
            var value = CombatVisualProfiles.For(card);
            Check(value != null && motifs.Contains(value.motif) && motions.Contains(value.motion), "recognized visual shape and motion: " + card.id);
            Check(value.duration >= .65f && value.duration <= 1.05f && value.impactAt >= .32f && value.impactAt <= .5f,
                "short effect duration and normalized readable impact: " + card.id);
            Check(value.projectiles >= 1 && value.projectiles <= 6 && value.seed >= 0, "bounded visual particles and stable nonnegative seed: " + card.id);
            Check(value.primary.Length == 6 && value.accent.Length == 6
                && value.primary.All(Uri.IsHexDigit) && value.accent.All(Uri.IsHexDigit), "valid source-inspired RGB palette: " + card.id);
            Check(value.ultimate == (card.key == "R"), "ultimate emphasis follows the actual R key: " + card.id);
            Check(card.movement == displacement.Contains(value.motion), "sprite movement follows the canonical movement-card classification: " + card.id);
            Check(ReferenceEquals(value, CombatVisualProfiles.For(card)), "the immutable visual profile is cached: " + card.id);
            Check(original[card.id] == VisualCardSnapshot(card), "looking up visuals never edits gameplay definitions: " + card.id);
            seenMotifs.Add(value.motif); seenMotions.Add(value.motion); seenColors.Add(value.primary + value.accent);
        }
        Check(seenMotifs.SetEquals(motifs), "the real catalog uses all 32 semantic effect families, including support and installation effects");
        Check(seenMotions.SetEquals(motions), "the real catalog covers every authored motion style");
        Check(seenColors.Count > 250, "per-skill source palettes do not collapse into a few hash-selected colors");
        Check(engine.State.rngState == random && engine.State.draftSeed == draftRandom && engine.State.deck.Count == deck,
            "full-catalog visual lookup does not consume run or draft RNG or change a player's deck");

        VisualIdentity("nia_q", "arcade", "cast"); VisualIdentity("nia_w", "orb", "cast");
        VisualIdentity("nia_e", "arcade", "cast"); VisualIdentity("nia_r", "arcade", "cast");
        foreach (var key in new[] { "q", "w", "e", "r" })
        {
            Check(CombatVisualProfiles.For(GameDatabase.Card("rio_" + key)).motif == "arrow", "Rio retains bow/arrow identity: " + key);
            Check(CombatVisualProfiles.For(GameDatabase.Card("adela_" + key)).motif == "chess", "Adela retains chess identity: " + key);
        }
        VisualIdentity("celine_q", "bomb", "cast"); VisualIdentity("celine_w", "bomb", "cast");
        VisualIdentity("theodore_w", "prism", "cast"); VisualIdentity("theodore_q", "sniper", "cast");
        VisualIdentity("yuki_q", "slash", "strike"); VisualIdentity("yuki_e", "slash", "dash");
        VisualIdentity("hyejin_e", "paper", "blink"); VisualIdentity("rio_e", "arrow", "leap");
        VisualIdentity("craver_e", "bullet", "retreat"); VisualIdentity("irem_r", "star", "orbit");
        VisualIdentity("echion_r", "prism", "cast"); VisualIdentity("blair_r", "blade_orbit", "cast");
        VisualIdentity("henry_e", "time", "blink"); VisualIdentity("weapon_camera", "camera", "cast");
        VisualIdentity("weapon_guitar", "music", "cast"); VisualIdentity("basic_guard", "shield", "none");
        Check(CombatVisualProfiles.For(GameDatabase.Card("hyejin_r")).projectiles == 5, "Five Omens has five visual charms");
        Check(CombatVisualProfiles.For(GameDatabase.Card("lucia_w")).projectiles == 4, "Lucia's volley has four summoned musket shots");
        Check(CombatVisualProfiles.For(GameDatabase.Card("tsubame_q")).projectiles == 3, "Tsubame throws three visual shuriken");
        Check(CombatVisualProfiles.For(GameDatabase.Card("nia_q")).projectiles == 1, "Arcade Drop creates one visual block, not three resource stacks");

        string nia = CombatVisualProfiles.For(GameDatabase.Card("nia_q")).primary;
        string blair = CombatVisualProfiles.For(GameDatabase.Card("blair_q")).primary;
        string echion = CombatVisualProfiles.For(GameDatabase.Card("echion_q")).primary;
        Check(VisualChannel(nia, 0) > VisualChannel(nia, 1) && VisualChannel(nia, 2) > VisualChannel(nia, 1), "Nia uses the original magenta arcade effects");
        Check(VisualChannel(blair, 1) > VisualChannel(blair, 0) && VisualChannel(blair, 2) > VisualChannel(blair, 0), "Blair uses the original cyan blade effects");
        Check(VisualChannel(echion, 0) > VisualChannel(echion, 1), "Echion's original crimson VF remains distinct from Blair's cyan blades");

        var copy = new CardDef { id = "nia_q", key = "Q", damage = 1 };
        var independent = CombatVisualProfiles.For(copy);
        Check(independent.seed == CombatVisualProfiles.For(GameDatabase.Card("nia_q")).seed
            && independent.motif == "arcade", "equivalent card IDs reproduce their visual identity independently of allocation or session");
        var future = new CardDef { id = "future_subject_q", key = "Q", movement = true, hits = 99, damage = 3 };
        var fallback = CombatVisualProfiles.For(future);
        Check(fallback.motion == "dash" && fallback.projectiles == 6 && fallback.motif == "barrage", "future definitions use a bounded semantic fallback");
        Check(CombatVisualProfiles.For(null) != null && CombatVisualProfiles.For(new CardDef()) != null,
            "missing and incomplete definitions are safe during presentation");
    }

    private static void VisualIdentity(string id, string motif, string motion)
    {
        var value = CombatVisualProfiles.For(GameDatabase.Card(id));
        Check(value.motif == motif && value.motion == motion, "original skill silhouette and movement identity: " + id);
    }

    private static int VisualChannel(string rgb, int index) { return Convert.ToInt32(rgb.Substring(index * 2, 2), 16); }
    private static string VisualCardSnapshot(CardDef card)
    {
        return string.Join("|", card.id, card.name, card.owner, card.key, card.description, card.category,
            card.cooldown, card.cost, card.damage, card.block, card.heal, card.draw, card.energy, card.poison,
            card.vulnerable, card.weak, card.strength, card.evasion, card.duration, card.hits, card.exhaust,
            card.movement, card.effect, card.effectColor, card.effectVariant, card.freeCastCount, card.freeCastOnHit);
    }
}
#endif
