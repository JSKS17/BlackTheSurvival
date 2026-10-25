using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia.Editor
{
    // Disposable browser fixtures are exported alongside a release build, never player saves.
    public static class WickelineReleaseBuild
    {
        public static void Build()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-lumia-fixture-output");
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Explicit fixture output required.");
            Export(Path.GetFullPath(args[index + 1]));
            LumiaWebBuild.Build();
        }

        static GameEngine Ready()
        {
            var e = new GameEngine(62173);
            var main = GameDatabase.Runes.First(r => r.main);
            var support = GameDatabase.Runes.First(r => !r.main && r.tree == main.tree);
            e.SetRunes(main.id, support.id);
            e.SelectStartingPassive(e.State.passiveOffers[0]);
            foreach (string id in e.State.draftOffers.Take(3).ToArray()) e.ToggleDraft(id);
            if (!e.BeginJourney()) throw new InvalidOperationException("Fixture preparation failed.");
            var node = e.State.map.Single(n => n.act == 3 && n.wickeline);
            e.State.act = 3; e.State.row = node.row - 1; e.State.lane = node.lane;
            e.State.activeNodeId = -1; e.State.stage = RunStage.Map;
            e.State.hp = e.State.maxHp = 120; e.State.credits = 50;
            return e;
        }

        static GameEngine Fight()
        {
            var e = Ready();
            var node = e.State.map.Single(n => n.act == 3 && n.wickeline);
            if (!e.EnterNode(node.lane) || !e.IsWickelineCombat) throw new InvalidOperationException("Fixture did not enter actual Wickeline combat.");
            return e;
        }

        static void NextTurn(GameEngine e)
        {
            if (!e.EndTurn()) throw new InvalidOperationException("Fixture turn failed.");
            int guard = 0;
            while (e.State.combat != null && e.State.combat.enemyTurn && guard++ < 30) e.AdvanceEnemyAction();
            if (!e.IsWickelineCombat || guard >= 30) throw new InvalidOperationException("Fixture enemy turn failed.");
        }

        static void Save(string directory, string name, GameEngine e)
        {
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonUtility.ToJson(e.State, true));
        }

        static void Export(string directory)
        {
            Directory.CreateDirectory(directory);
            Save(directory, "wickeline-map", Ready());
            var e = Fight(); Save(directory, "wickeline-combat", e);
            NextTurn(e); Save(directory, "wickeline-gas", e);
            NextTurn(e); NextTurn(e); NextTurn(e); Save(directory, "wickeline-isolation", e);
            if (!WickelineMechanics.Isolating(e.State.combat)) throw new InvalidOperationException("Isolation fixture did not charge.");
            e = Fight();
            e.State.combat.enemyHp = 1; e.State.combat.enemyBlock = 0; e.State.combat.enemyEvasion = 0;
            e.State.combat.hand.Clear(); e.State.combat.hand.Add("basic_attack");
            e.State.combat.energy = 3;
            Save(directory, "wickeline-victory-ready", e);
            if (!e.PlayCard(0) || e.State.stage != RunStage.Campfire || !e.State.objects.Contains("blood"))
                throw new InvalidOperationException("Wickeline fixture victory did not reach rewarded campfire.");
            Save(directory, "wickeline-campfire", e);
            Debug.Log("WICKELINE_BROWSER_FIXTURES / six actual engine states / " + directory);
        }
    }
}
