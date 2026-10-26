using System;
using System.Reflection;
using Lumia;
public static class InspectRewardFixture
{
    public static void Main()
    {
        var tests = typeof(GameEngine).Assembly.GetType("CoreSmokeTests");
        var engine = (GameEngine)tests.GetMethod("Ready", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 771 });
        tests.GetMethod("Enter", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { engine, ZoneKind.Subject, 1 });
        tests.GetMethod("Win", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { engine });
        var reward = engine.State.rewards;
        int node = engine.State.activeNodeId, credit = engine.State.credits, xp = engine.State.xp, deck = engine.State.deck.Count;
        string card = reward.choices[0]; engine.ClaimCard(card);
        Console.WriteLine("BEFORE stage=" + engine.State.stage + ",row=" + engine.State.row + ",lumiAppears=" + reward.lumiAppears + ",lumiRolled=" + reward.lumiRolled + ",cardsCommitted=" + reward.cardsCommitted);
        bool first = engine.FinishRewards(), second = engine.FinishRewards();
        Console.WriteLine("AFTER first=" + first + ",second=" + second + ",stage=" + engine.State.stage + ",row=" + engine.State.row + ",sameNode=" + (engine.State.activeNodeId == node) + ",creditsStable=" + (engine.State.credits == credit) + ",xpStable=" + (engine.State.xp == xp) + ",deckAdded=" + (engine.State.deck.Count - deck));
        bool sent = engine.SendLumiAway();
        Console.WriteLine("AFTER LUMI sent=" + sent + ",stage=" + engine.State.stage + ",row=" + engine.State.row + ",repeatReward=" + engine.FinishRewards() + ",creditsStable=" + (engine.State.credits == credit) + ",xpStable=" + (engine.State.xp == xp) + ",deckAdded=" + (engine.State.deck.Count - deck));
    }
}
