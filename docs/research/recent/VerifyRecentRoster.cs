using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;
namespace Lumia { public static class GameDatabase { public static int CostForCooldown(float seconds) { return seconds <= 2 ? 0 : seconds <= 6 ? 1 : seconds <= 11 ? 2 : seconds <= 20 ? 3 : seconds <= 35 ? 4 : seconds <= 50 ? 5 : seconds <= 75 ? 6 : seconds <= 100 ? 7 : 8; } } }
public static class VerifyRecentRoster {
    static void Check(bool condition, string message) { if(!condition) throw new Exception(message); }
    public static void Main() {
        var cards=new List<CardDef>(); var passives=new List<PassiveDef>(); var characters=new List<CharacterDef>();
        RecentRoster.Populate(cards,passives,characters);
        Check(cards.Count==104,"Expected 104 QWER cards."); Check(passives.Count==26,"Expected 26 passives."); Check(characters.Count==26,"Expected 26 subjects.");
        Check(cards.Select(x=>x.id).Distinct().Count()==cards.Count,"Duplicate card ID.");
        Check(passives.Select(x=>x.id).Distinct().Count()==passives.Count,"Duplicate passive ID.");
        foreach(var ch in characters) {
            Check(ch.cards.Length==4,"Missing four skills for "+ch.id);
            Check(ch.cards.Select(id=>cards.Single(c=>c.id==id).key).OrderBy(k=>k).SequenceEqual(new[]{"E","Q","R","W"}),"Incorrect skill keys for "+ch.id);
            Check(passives.Any(p=>p.id==ch.passiveId && p.owner==ch.name),"Missing owned passive for "+ch.id);
        }
        foreach(var c in cards) {
            Check(c.damage>=0 && c.block>=0 && c.heal>=0 && c.cost>=0,"Invalid values for "+c.id);
            foreach(var target in c.freeCastTargets) Check(cards.Any(t=>t.id==target && t.owner==c.owner),"Invalid combo target "+target);
        }
        Check(cards.Single(c=>c.id=="tsubame_r").cost==3,"Tsubame mark restriction was lost.");
        Check(cards.Single(c=>c.id=="justyna_q").cost==2,"Justyna resource restriction was lost.");
        Check(cards.Single(c=>c.id=="hisui_w").cost==3,"Hisui stack restriction was lost.");
        Check(characters.Single(c=>c.id=="charlotte").name=="샬럿","Charlotte is missing.");
        Check(cards.Single(c=>c.id=="ceres_r").name=="빛에게 바치는 맹세","Newest release is missing.");
        var registered = new List<CharacterDef>{new CharacterDef{id="charlotte",name="샬럿"}};
        RecentRoster.Populate(new List<CardDef>(),new List<PassiveDef>(),registered);
        Check(registered.Count==26,"Existing event subjects were duplicated.");
        Check(registered.Single(c=>c.id=="charlotte").cards.Length==4,"Existing subject was not filled.");
        Console.WriteLine("PASS: 26 subjects, 104 real QWER cards, 26 passives, all skill keys and combo IDs valid; existing character fill and resource cost exceptions checked.");
    }
}