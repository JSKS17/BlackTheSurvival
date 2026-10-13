#if LUMIA_TEST_RUNNER
using System;
using System.Linq;
using System.Collections.Generic;
using Lumia;

public static partial class CoreSmokeTests
{
    static void TutorialRulesAndIsolation()
    {
        var real = Ready(84111); var before = (RunState)Clone(real.State); int rng = real.State.rngState;
        var lesson = new TutorialSession(); var engine = lesson.Engine; var c = engine.State.combat;
        Check(lesson.Step == TutorialStep.Welcome && !lesson.Play(0) && !lesson.EndTurn(), "welcome blocks actions until lesson start");
        Check(lesson.Start() && !lesson.Start() && !lesson.Play(1), "lesson starts once and enforces the announced card");
        int target = c.enemyHp;
        Check(lesson.Play(0) && target - c.enemyHp == engine.CardDamage("basic_attack") && c.energy == 4, "basic attack demonstrates actual damage and cost");
        Check(lesson.Play(0) && c.block == GameDatabase.Card("basic_guard").block && c.energy == 3, "guard uses real block and cost");
        int incoming = engine.PreviewCardUse("basic_attack",true).normalDamage;
        Check(!lesson.Play(0) && lesson.EndTurn() && !lesson.EndTurn(), "lesson end turn starts once");
        int hp = engine.State.hp;
        while (c.enemyTurn) engine.AdvanceEnemyAction(); lesson.ObserveTurn();
        Check(lesson.Step == TutorialStep.FirstBlock && c.turn == 2 && c.energy == 5 && c.block == 0
            && hp - engine.State.hp == Math.Max(0, incoming - GameDatabase.Card("basic_guard").block), "real enemy turn resolves block and refreshes energy");
        Check(!lesson.Play(2) && lesson.Play(0) && SkillMechanics.Resource(c.playerSkills,"니아","blocks") == 1, "first Q creates one field block and disallows wrong lesson action");
        Check(lesson.Play(0) && SkillMechanics.Resource(c.playerSkills,"니아","blocks") == 2, "second Q creates block value two");
        Check(lesson.Play(c.hand.IndexOf("basic_guard")), "guard spends the last spare practice cost");
        target = c.enemyHp;
        Check(lesson.Play(c.hand.IndexOf("nia_w")) && c.energy == 0 && SkillMechanics.Resource(c.playerSkills,"니아","blocks") == 0
            && target - c.enemyHp >= GameDatabase.Card("nia_w").damage + 8 && engine.EffectiveCardCost("nia_q") == 0, "W consumes two blocks, deals scaled damage and discounts Q at zero remaining cost");
        Check(lesson.Play(c.hand.IndexOf("nia_q")) && c.energy == 0 && SkillMechanics.Resource(c.playerSkills,"니아","blocks") == 1, "discounted zero cost Q resolves through the normal game engine");
        Check(!lesson.FinishField() && lesson.OpenField() && lesson.FinishField() && lesson.Step == TutorialStep.Complete, "field confirmation is part of lesson completion");
        Check(real.State.rngState == rng && real.State.hp == before.hp && real.State.credits == before.credits
            && real.State.deck.SequenceEqual(before.deck) && real.State.stage == before.stage, "practice receives no live run reference and cannot alter live progression");
        Check(TutorialSession.Dialogue.Length == Enum.GetValues(typeof(TutorialStep)).Length && TutorialSession.Dialogue.All(line=>!string.IsNullOrWhiteSpace(line)), "every tutorial step has Nia dialogue");
    }
    static void LobbyRandomDialogueIsolation()
    {
        var live = Ready(84112); int rng = live.State.rngState;
        var firstLines = new HashSet<int>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var bag = new LobbyDialogueShuffle(seed); int previous = -1;
            for (int cycle = 0; cycle < 4; cycle++)
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < LobbyDialogue.Count; i++)
                {
                    int value = bag.Next(); if (cycle == 0 && i == 0) firstLines.Add(value);
                    Check(value >= 0 && value < LobbyDialogue.Count && value != previous && seen.Add(value), "shuffle visits all lines once and avoids immediate repeats across bag boundaries");
                    previous = value;
                }
            }
        }
        Check(firstLines.Count > 5 && live.State.rngState == rng, "lobby starts vary without using the game's random stream");
    }
}
#endif
