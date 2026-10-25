using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia.Editor
{
    // Optional disposable account fixtures for browser validation of the release.
    public static class LumiReleaseBuild
    {
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-lumia-fixture-output");
            if (index >= 0 && index + 1 < args.Length) Export(Path.GetFullPath(args[index + 1]));
            LumiaWebBuild.Build();
        }

        static GameEngine Ready(bool nearKiosk)
        {
            var engine = new GameEngine(62173);
            var main = GameDatabase.Runes.First(r => r.main);
            var support = GameDatabase.Runes.First(r => !r.main && r.tree == main.tree);
            engine.SetRunes(main.id, support.id);
            engine.SelectStartingPassive(engine.State.passiveOffers[0]);
            foreach (string id in engine.State.draftOffers.Take(3).ToArray()) engine.ToggleDraft(id);
            engine.BeginJourney();
            foreach (var node in engine.State.map.Where(n => n.kind == ZoneKind.Kiosk)) node.kind = ZoneKind.Encounter;
            var active = engine.AvailableNodes().First(n => n.lane == 1);
            active.kind = ZoneKind.Subject;
            if (nearKiosk) engine.State.map.First(n => n.act == active.act && n.row == active.row + 1 && n.lane == active.lane).kind = ZoneKind.Kiosk;
            engine.State.activeNodeId = active.id;
            engine.State.stage = RunStage.Rewards;
            engine.State.rewards = new RewardState { source = RewardSource.Subject, lumiRolled = true, lumiAppears = true };
            engine.State.credits = 2000;
            if (!engine.FinishRewards()) throw new InvalidOperationException("Lumi fixture reward did not finish.");
            return engine;
        }

        static void Save(string directory, string name, GameEngine engine)
        {
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonUtility.ToJson(engine.State, true));
        }

        static void Export(string directory)
        {
            Directory.CreateDirectory(directory);
            var engine = Ready(false); Save(directory, "lumi-offer", engine);
            engine.CatchLumi(); Save(directory, "lumi-shop", engine);
            engine = Ready(true); engine.CatchLumi(); Save(directory, "lumi-combat", engine);
            engine = Ready(false);
            engine.State.map.Find(node => node.id == engine.State.activeNodeId).kind = ZoneKind.Encounter;
            engine.State.rewards = null;
            engine.State.stage = RunStage.Encounter;
            engine.State.research.unlockedReports.Add("lumi");
            engine.State.chosenEventId = ResearchEncounters.EventForSubject("lumi").id;
            engine.State.encounterOffers.Clear(); engine.State.encounterOffers.Add(engine.State.chosenEventId);
            Save(directory, "lumi-rare", engine);
            engine = Ready(false);
            var niaNode = engine.State.map.Find(node => node.id == engine.State.activeNodeId);
            niaNode.kind = ZoneKind.Subject; niaNode.assignedOpponent = "nia";
            engine.State.stage = RunStage.Map; engine.State.rewards = null;
            if (!engine.EnterNode(niaNode.lane) || engine.State.combat == null || engine.State.combat.enemyId != "nia")
                throw new InvalidOperationException("Rare appearance combat fixture did not enter Nia combat.");
            Save(directory, "nia-rare-combat", engine);
            var profile = new ResearchProfile();
            profile.data = 2000;
            profile.totalData = 2000;
            profile.completedReports.Add("nia");
            profile.completedReports.Add("lumi");
            profile.rareAppearanceSubjects.Add("nia");
            File.WriteAllText(Path.Combine(directory, "research.json"), JsonUtility.ToJson(profile, true));
            Debug.Log("LUMI_BROWSER_FIXTURES " + directory);
        }
    }
}
