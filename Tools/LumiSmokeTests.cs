#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine LumiRewardFixture(bool nearKiosk, bool wildlife = false)
    {
        var engine = Ready(62173);
        foreach (var candidate in engine.State.map.Where(n => n.kind == ZoneKind.Kiosk)) candidate.kind = ZoneKind.Encounter;
        var node = engine.AvailableNodes().First(n => n.lane == 1);
        node.kind = wildlife ? ZoneKind.Wildlife : ZoneKind.Subject;
        if (nearKiosk) engine.State.map.First(n => n.act == node.act && n.row == node.row + 1 && n.lane == node.lane).kind = ZoneKind.Kiosk;
        engine.State.activeNodeId = node.id;
        engine.State.stage = RunStage.Rewards;
        engine.State.rewards = new RewardState { source = wildlife ? RewardSource.Wildlife : RewardSource.Subject, lumiRolled = true, lumiAppears = true };
        return engine;
    }

    static void LumiMechanics()
    {
        Check(GameEngine.LumiEncounterChance == 3 && GameEngine.LumiTurnLimit == 3, "Lumi uses requested rare roll and exact three-turn limit");
        foreach (bool wildlife in new[] { false, true })
        {
            var engine = LumiRewardFixture(false, wildlife);
            int nodeId = engine.State.activeNodeId;
            Check(engine.FinishRewards() && engine.State.stage == RunStage.LumiOffer, "both subject and wildlife victories may lead to Lumi");
            Check(engine.State.rewards.cardsCommitted && engine.State.rewards.lumiRolled && !engine.State.rewards.lumiAppears, "ordinary rewards and Lumi appearance are committed once");
            var saved = new GameEngine((RunState)Clone(engine.State));
            int observations = saved.State.research.lumiDiscoveries;
            Check(!saved.FinishRewards() && saved.State.research.lumiDiscoveries == observations, "restored Lumi offer cannot duplicate reward or discovery");
            Check(saved.SendLumiAway() && saved.State.stage == RunStage.Map && saved.State.map.First(n => n.id == nodeId).visited, "passing Lumi completes the original battle node");
            Check(!saved.SendLumiAway() && !saved.CatchLumi(), "completed Lumi offer cannot be reopened");
        }

        var shop = LumiRewardFixture(false);
        Check(shop.FinishRewards() && shop.CatchLumi() && shop.State.stage == RunStage.LumiShop, "outside kiosk proximity catching Lumi opens the shop");
        Check(shop.LumiItemPrice("telescope_camera") == 20 && shop.LumiItemPrice("tactical_module") == 160
            && shop.LumiItemPrice("portable_kiosk") == 100 && shop.LumiObjectPrice == GameDatabase.Object(shop.LumiObjectId).price - 40, "all four Lumi prices follow requested fixed/base discount values");
        string objectId = shop.LumiObjectId;
        shop.State.supportRune = "coupon"; shop.State.credits = 2000;
        Check(shop.LumiObjectPrice == GameDatabase.Object(objectId).price - 40, "coupon cannot further reduce Lumi's object price");
        foreach (string item in new[] { "telescope_camera", "tactical_module", "object", "portable_kiosk" })
        {
            int credits = shop.State.credits, price = shop.LumiItemPrice(item);
            Check(shop.BuyLumiItem(item) && shop.State.credits == credits - price && shop.LumiItemSold(item), "Lumi transaction charges once and marks its slot: " + item);
            Check(!shop.BuyLumiItem(item) && shop.State.credits == credits - price, "purchased slot cannot duplicate inventory: " + item);
        }
        Check(shop.State.telescopicCameras == 1 && shop.State.portableKiosks == 1 && shop.State.tacticalModules == 1 && shop.State.objects.Contains(objectId), "all Lumi purchases enter the appropriate existing inventory");
        var loadedShop = new GameEngine((RunState)Clone(shop.State));
        Check(loadedShop.LumiObjectId == objectId && loadedShop.LumiItemSold("object") && loadedShop.State.research.lumiObjectPurchases == 1, "save/load preserves object offering, sold stock and purchase research");
        Check(loadedShop.LeaveLumiShop() && loadedShop.State.stage == RunStage.Map, "Lumi merchant close completes only its original node");

        var challenge = LumiRewardFixture(true);
        Check(challenge.FinishRewards() && challenge.CatchLumi() && challenge.IsLumiCombat, "kiosk-adjacent Lumi begins the damage challenge");
        var combat = challenge.State.combat;
        Check(combat.turn == 1 && combat.enemyDeck.Count == 0 && combat.enemyPlan.Count == 0 && combat.intentDamage == 0, "Lumi has no attack deck or planned damage");
        var damageMethod = typeof(GameEngine).GetMethod("ApplyExternalHealthDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        int creditsBefore = challenge.State.credits, hpBefore = challenge.State.hp;
        damageMethod.Invoke(challenge, new object[] { true, 39, false });
        Check(combat.lumiDamage == 39 && challenge.State.credits == creditsBefore, "partial damage accumulates without rounding up a credit payout");
        damageMethod.Invoke(challenge, new object[] { true, 1, false });
        Check(combat.lumiDamage == 40 && combat.lumiCredits == 20 && challenge.State.credits == creditsBefore + 20, "exact 40 damage grants exactly 20 credits");
        damageMethod.Invoke(challenge, new object[] { true, 85, false });
        Check(combat.lumiDamage == 125 && combat.lumiCredits == 60 && challenge.State.research.lumiCreditsEarned == 60, "multi-threshold damage and research use actual cumulative payouts");
        int xp = challenge.State.xp, deckCount = challenge.State.deck.Count, victories = challenge.State.research.victoryNodeIds.Count;
        Check(challenge.EndTurn() && challenge.IsLumiCombat && combat.turn == 2 && challenge.State.hp == hpBefore, "first Lumi turn produces no enemy attack");
        var loadedChallenge = new GameEngine((RunState)Clone(challenge.State));
        Check(loadedChallenge.State.combat.lumiDamage == 125 && loadedChallenge.State.combat.lumiCredits == 60, "damage score and credit acknowledgement survive challenge reload");
        Check(loadedChallenge.EndTurn() && loadedChallenge.IsLumiCombat && loadedChallenge.State.combat.turn == 3, "challenge reaches exactly its third player turn");
        Check(loadedChallenge.EndTurn() && loadedChallenge.State.stage == RunStage.Map && loadedChallenge.State.combat == null, "third player turn forcibly closes the challenge");
        Check(loadedChallenge.State.xp == xp && loadedChallenge.State.deck.Count == deckCount && loadedChallenge.State.research.victoryNodeIds.Count == victories, "Lumi does not grant ordinary battle rewards, XP or an extra combat victory");

        loadedShop.State.telescopicCameras = 1;
        int cameraCredits = loadedShop.State.credits;
        Check(loadedShop.CanUseTelescopicCamera && loadedShop.UseTelescopicCamera() && loadedShop.State.telescopicCameras == 0 && loadedShop.State.credits == cameraCredits, "camera reveals normal two-row intelligence without charging security-console credits");
        Check(!loadedShop.UseTelescopicCamera() && !loadedShop.CanUseSecurityConsole, "camera and security-console reveal share the per-act observation limit");
        loadedShop.State.portableKiosks = 1;
        int row = loadedShop.State.row, nodeBefore = loadedShop.State.activeNodeId;
        Check(loadedShop.UsePortableKiosk() && loadedShop.State.stage == RunStage.Kiosk && loadedShop.State.portableKiosks == 0, "portable kiosk consumes once and opens normal kiosk stock");
        var restoredKiosk = new GameEngine((RunState)Clone(loadedShop.State));
        Check(restoredKiosk.LeaveKiosk() && restoredKiosk.State.stage == RunStage.Map && restoredKiosk.State.row == row && restoredKiosk.State.activeNodeId == nodeBefore, "portable kiosk reload and close do not advance or complete a map node");
        var camp = Ready(); Enter(camp, ZoneKind.Campfire); camp.ChooseCamp(false); camp.State.portableKiosks = 1;
        Check(camp.UsePortableKiosk(), "portable kiosk can be used while resting");
        camp = new GameEngine((RunState)Clone(camp.State));
        Check(camp.LeaveKiosk() && camp.State.stage == RunStage.Campfire && camp.State.campRemovalsRemaining == 2, "portable kiosk preserves the campfire's saved actions and removal budget");
    }
}
#endif
