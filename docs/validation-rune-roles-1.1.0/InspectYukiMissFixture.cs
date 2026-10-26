using System;
using System.Linq;
using System.Reflection;
using Lumia;
public static class InspectYukiMissFixture
{
    public static void Main()
    {
        var tests = typeof(GameEngine).Assembly.GetType("SharedPassiveResourceSmokeTests");
        var fixture = tests.GetMethod("Fixture", BindingFlags.Static | BindingFlags.NonPublic);
        var cast = tests.GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic);
        var engine = (GameEngine)fixture.Invoke(null, new object[] { "yuki_p", "fixture" });
        var c = engine.State.combat;
        foreach (string card in new[] { "yuki_q", "yuki_q", "yuki_w" }) cast.Invoke(null, new object[] { engine, card });
        c.enemyEvasion = 100;
        int target = c.enemyHp, resource = SkillMechanics.Resource(c.playerSkills, GameDatabase.Character("yuki").name, "cuff");
        Console.WriteLine("BEFORE rawEvasion=" + c.enemyEvasion + ",effectiveEvasion=" + engine.EnemyEvasion + ",statuses=" + string.Join(",", c.enemyStatuses.effects.Select(effect => effect.kind)) + ",buttons=" + resource + ",rng=" + engine.State.rngState);
        cast.Invoke(null, new object[] { engine, "yuki_q" });
        Console.WriteLine("AFTER damage=" + (target - c.enemyHp) + ",buttons=" + SkillMechanics.Resource(c.playerSkills, GameDatabase.Character("yuki").name, "cuff"));
        engine = (GameEngine)fixture.Invoke(null, new object[] { "yuki_p", "fixture" }); c = engine.State.combat;
        foreach (string card in new[] { "yuki_q", "yuki_q", "yuki_w" }) cast.Invoke(null, new object[] { engine, card });
        c.enemyStatuses = new StatusActorState(); c.enemyEvasion = 100; target = c.enemyHp;
        Console.WriteLine("ISOLATED BEFORE effectiveEvasion=" + engine.EnemyEvasion);
        cast.Invoke(null, new object[] { engine, "yuki_q" });
        Console.WriteLine("ISOLATED AFTER damage=" + (target - c.enemyHp) + ",buttons=" + SkillMechanics.Resource(c.playerSkills, GameDatabase.Character("yuki").name, "cuff"));
    }
}
