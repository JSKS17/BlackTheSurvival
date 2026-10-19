#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void TacticalSkillMechanics()
    {
        Check(TacticalSkills.Definitions.Length==26 && TacticalSkills.Definitions.Select(x=>x.id).Distinct().Count()==26,"26 named original tactical identities, no duplicate common cobalt versions");
        Check(TacticalSkills.CardDefinitions.Count()==26 && !TacticalSkills.Definitions.Any(x=>x.sourceCode==70 || string.IsNullOrEmpty(x.name)),"transcendence and empty removed placeholders are excluded");
        foreach(var def in TacticalSkills.Definitions)
        {
            var card=GameDatabase.Card(def.id);string frozen=FieldSnapshot(card);
            Check(card!=null && card.key=="F" && card.category=="tactical" && card.cost>=1,"registered tactical is playable: "+def.id);
            var enhanced=TacticalSkills.ApplyModuleUpgrade(card);
            Check(enhanced!=card && enhanced.tacticalEnhanced && FieldSnapshot(card)==frozen,"module never edits source: "+def.id);
            Check(FieldSnapshot(TacticalSkills.ApplyModuleUpgrade(enhanced))==FieldSnapshot(enhanced),"module modifier is idempotent: "+def.id);
            Check(!string.IsNullOrWhiteSpace(TacticalSkills.Brief(card)) && !string.IsNullOrWhiteSpace(card.description) && !string.IsNullOrWhiteSpace(TacticalSkills.ModuleUpgradeDescription(card)),"summary/full/module explanations exist: "+def.id);
            Check(DescriptionSummary.PreviewWidth(TacticalSkills.Brief(enhanced))<=DescriptionSummary.CardPreviewMaxWidth && DescriptionSummary.PreviewLines(TacticalSkills.Brief(enhanced))<=DescriptionSummary.CardPreviewMaxLines,"module card summary fits: "+def.id);
            var e=AdvancedFixture(card.id);var c=e.State.combat; c.enemyEvasion=0;
            Check(e.PlayCard(0),"actual player casts tactical: "+def.id);
            Check(c.energy>=0 && e.State.hp>0 && e.State.stage==RunStage.Combat,"tactical keeps valid bounded combat: "+def.id);
            var saved=new GameEngine((RunState)Clone(e.State));
            Check(FieldSnapshot(saved.State.combat.playerSkills)==FieldSnapshot(c.playerSkills),"tactical state survives save/load: "+def.id);
            e=AdvancedFixture();AdvancedEnemyCast(e,card.id);
            Check(e.State.hp>0 && e.State.combat.enemyHp>0,"enemy executes identical tactical identity: "+def.id);
        }
        TacticalDelayAndStasis();TacticalTrueDamageAndCleanse();TacticalFollowupAndResources();TacticalForecastAndCaps();TacticalSummarySemantics();
    }
    static void TacticalDelayAndStasis()
    {
        foreach(var id in new[]{"tactical_gravity","tactical_impulse","tactical_meteor"})
        {
            var card=TacticalSkills.ApplyModuleUpgrade(GameDatabase.Card(id));var actor=new SkillActorState();
            SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),card,true);
            Check(SkillMechanics.Tick(actor).Count==0 && actor.effects.Any(x=>x.kind=="tactical_pending"),"delayed tactical does not fire at cast-turn end: "+id);
            var opening=TacticalSkills.BeginTurn(actor);
            Check(opening.pulses.Count==1 && opening.pulses[0].amount==card.mechanics.rules[0].amount,"next turn preserves full enhanced delayed damage without ordinary 24 cap: "+id);
            Check(TacticalSkills.BeginTurn(actor).pulses.Count==0,"delayed tactical fires exactly once: "+id);
            Check(SkillMechanics.Rules(card).Contains("다음 자기 턴 시작") && !SkillMechanics.Rules(card).Contains("턴 종료에 적용"),"delayed rules match actual timing: "+id);
            var e=AdvancedFixture(id);var c=e.State.combat;
            Check(e.PlayCard(0),"arm real delayed tactical: "+id);int hp=c.enemyHp;
            Check(e.BeginEndTurn() && c.enemyHp==hp,"real delayed tactical does not fire early: "+id);
            Check(e.AdvanceEnemyAction() && c.enemyHp<hp,"real delayed tactical fires at player start: "+id);
        }
        var game=AdvancedFixture("tactical_artifact","basic_attack");var combat=game.State.combat;
        Check(game.PlayCard(0) && !game.CanPlayCard(0),"artifact prevents paid and free further actions");
        var preview=game.PreviewCardUse(0);
        Check(!preview.canUse && preview.unavailableReason.Contains("경직"),"card use information names artifact restriction");
        combat.poison=12;int hpBefore=game.State.hp;
        Check(game.EndTurn() && game.State.hp<hpBefore,"artifact expires before next-turn poison, rather than lasting indefinitely");
        Check(TacticalSkills.CanAct(combat.playerSkills),"artifact unlocks next own turn");
        var actor2=new SkillActorState();var enhancedArtifact=TacticalSkills.ApplyModuleUpgrade(GameDatabase.Card("tactical_artifact"));
        SkillMechanics.AfterCard(actor2,SkillMechanics.Clone(actor2),enhancedArtifact,true);
        Check(TacticalSkills.IncomingDamage(actor2,99)==0 && TacticalSkills.BeginTurn(actor2).reward==1 && TacticalSkills.IncomingDamage(actor2,99)==99,"enhanced artifact returns exactly one opening reward");
    }
    static void TacticalTrueDamageAndCleanse()
    {
        var e=AdvancedFixture("tactical_repulsor");var c=e.State.combat;c.enemyBlock=1000;int hp=c.enemyHp;
        Check(e.PlayCard(0) && c.enemyHp<hp && c.enemyBlock==1000,"repulsor true damage bypasses shield without destroying it");
        e=AdvancedFixture("tactical_nullification");c=e.State.combat;c.poison=5;c.weak=2;c.vulnerable=2;
        StatusMechanics.Add(c.playerStatuses,"blind","sua_w",2);StatusMechanics.Add(c.playerStatuses,"slow","hyunwoo_q",2);
        c.enemySkills.effects.Add(new SkillTimedEffect {kind="bleed",sourceCard="jackie_q",key="enemy_bleed",amount=4,remaining=2});
        c.enemyTraits.skills.effects.Add(new SkillTimedEffect {kind="burn",sourceCard="adriana_p",key="enemy_burn",amount=4,remaining=2});
        c.playerSkills.effects.Add(new SkillTimedEffect {kind="bleed",sourceCard="cathy_q",key="my_bleed",amount=4,remaining=2});
        Check(e.PlayCard(0) && c.poison==0 && c.weak==0 && c.vulnerable==0 && c.playerStatuses.effects.Count==0,"nullification cleanses legacy and named debuffs");
        Check(c.enemySkills.effects.All(x=>x.kind!="bleed") && c.enemyTraits.skills.effects.All(x=>x.kind!="burn") && c.playerSkills.effects.Any(x=>x.key=="my_bleed"),"cleanse removes incoming skill and passive DOT, preserving outgoing damage");
    }
    static void TacticalFollowupAndResources()
    {
        var e=AdvancedFixture("tactical_lock_on");var c=e.State.combat;
        Check(e.PlayCard(0) && c.hand.Contains("tactical_lock_on") && e.EffectiveCardCost("tactical_lock_on")==0,"lock-on recalls a free follow-up only after hit");
        Check(e.PlayCard(c.hand.IndexOf("tactical_lock_on")) && c.freeCasts.All(x=>x.uses<=0),"free lock-on cannot create an endless reset");
        e=AdvancedFixture("tactical_lock_on");c=e.State.combat;c.enemyEvasion=100;
        Check(e.PlayCard(0) && c.freeCasts.Count==0 && !c.hand.Contains("tactical_lock_on"),"missed lock-on creates no free follow-up");
        var actor=new SkillActorState();TacticalSkills.RecordDamage(actor,25);
        var soul=GameDatabase.Card("tactical_soul");var before=SkillMechanics.Clone(actor);
        Check(SkillMechanics.Bonuses(before,soul).heal==10,"soul healing is proportional to actually lost HP");
        SkillMechanics.AfterCard(actor,before,soul,true);
        Check(SkillMechanics.Resource(actor,"전술","soul_charge")==0 && TacticalSkills.LifeSteal(actor,24)==6,"soul consumes its meter and grants bounded damage-linked healing");
        var upgraded=TacticalSkills.ApplyModuleUpgrade(GameDatabase.Card("tactical_strider"));actor=new SkillActorState();
        SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),upgraded,true);
        Check(SkillMechanics.Bonuses(actor,GameDatabase.Card("nia_q")).damage==8,"strider empowers another subject's next damaging card");
        SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),GameDatabase.Card("nia_q"),true);
        Check(!TacticalSkills.Has(actor,"strider"),"strider consumes on the empowered landed attack");
    }
    static void TacticalForecastAndCaps()
    {
        foreach(var id in new[]{"tactical_asthenia","tactical_fissure"})
        {
            var card=TacticalSkills.ApplyModuleUpgrade(GameDatabase.Card(id));var target=new StatusActorState();
            StatusMechanics.Apply(target,new SkillActorState(),card,true);
            Check(target.effects.First(x=>x.kind=="slow").remaining==2,"enhanced slow really reaches shown two-turn duration: "+id);
        }
        var shield=TacticalSkills.ApplyModuleUpgrade(RareCardUpgrades.Apply(GameDatabase.Card("tactical_lightning_shield")));
        var skills=new SkillActorState();SkillMechanics.AfterCard(skills,SkillMechanics.Clone(skills),shield,true);
        Check(SkillMechanics.Counters(skills).Single().amount==shield.mechanics.rules[0].amount && shield.mechanics.rules[0].amount<=8,"rare plus module shield matches its real counter cap");
        foreach(var id in new[]{"tactical_protocol","tactical_gravity","tactical_impulse","tactical_meteor"})
        {
            var e=AdvancedFixture();var c=e.State.combat;var card=GameDatabase.Card(id);
            SkillMechanics.AfterCard(c.enemySkills,SkillMechanics.Clone(c.enemySkills),card,true);
            StatusMechanics.Apply(c.playerStatuses,new SkillActorState(),card,true);
            TraitMechanics.Ensure(c.enemyTraits);TraitMechanics.Ensure(c.playerTraits);
            var fields=new object[]{c.enemySkills,c.playerSkills,c.enemyTraits,c.playerTraits,c.playerStatuses};
            var stable=fields.Select(FieldSnapshot).ToArray();
            typeof(GameEngine).GetMethod("RefreshEnemyIntent",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e,null);
            Check(c.intentDamage>=card.mechanics.rules[0].amount,"enemy intent includes pending turn-start tactical damage: "+id);
            Check(stable.SequenceEqual(fields.Select(FieldSnapshot)),"forecast never consumes live tactical effects or statuses: "+id+" changed="+string.Join(",",fields.Select((f,i)=>FieldSnapshot(f)==stable[i]?"":i.ToString()).Where(x=>x.Length>0)));
            int hp=e.State.hp;Check(e.BeginEndTurn() && e.State.hp==hp-card.mechanics.rules[0].amount,"actual pending damage matches the forecast source amount: "+id);
        }
    }
    static void TacticalSummarySemantics()
    {
        foreach(var original in TacticalSkills.CardDefinitions)
        {
            var enhanced=TacticalSkills.ApplyModuleUpgrade(original);
            var variants=new List<CardDef> {original,enhanced,TacticalSkills.ApplyModuleUpgrade(RareCardUpgrades.Apply(original))};
            variants.AddRange(StrategyMechanics.Options(original).Select(option=>TacticalSkills.ApplyModuleUpgrade(StrategyMechanics.Apply(original,option.id))));
            foreach(var card in variants)
            foreach(bool live in new[]{false,true})
            {
                string text=DescriptionSummary.CardPreview(card,damage:live&&card.damage>0?99:-1,block:live&&card.block>0?99:-1,heal:live&&card.heal>0?99:-1);
                string label=card.id+" enhanced="+card.tacticalEnhanced+" live="+live;
                Check(DescriptionSummary.PreviewWidth(text)<=DescriptionSummary.CardPreviewMaxWidth && DescriptionSummary.PreviewLines(text)<=DescriptionSummary.CardPreviewMaxLines,"every tactical variant fits the real preview budget: "+label);
                if(card.damage>0)Check(text.StartsWith("피해 ",StringComparison.Ordinal) || text.StartsWith("고정 피해 ",StringComparison.Ordinal),"tactical direct damage remains the opening action: "+label);
                if(card.block>0)Check(text.Contains("방어") || text.Contains("보호"),"tactical preview retains protection: "+label);
                if(card.heal>0)Check(text.Contains("회복") || text.Contains("치유"),"tactical preview retains immediate recovery: "+label);
                if(card.draw>0)Check(text.Contains("뽑") || text.Contains("교환"),"tactical preview retains the draw action: "+label);
                if(card.evasion>0)Check(text.Contains("회피"),"tactical preview retains its movement evasion: "+label);
                foreach(var status in card.statuses)Check(text.Contains(StatusMechanics.Name(status.key)),"tactical preview names every real status: "+label+" "+status.key);
                foreach(var guard in card.mechanics.rules.Where(rule=>rule.op=="guard" && rule.key=="rare_guard"))Check(text.Contains("방어 +"+guard.amount) && text.Contains("턴 끝"),"tactical preview keeps rare utility guard: "+label);
                if(card.freeCastCount>0)Check(text.Contains("F") && text.Contains("이번 턴") && text.Contains(card.freeCastCount+"회") && text.Contains("무료"),"tactical preview keeps target, turn and finite free reuse: "+label);
                if(TacticalSkills.IsDelayed(card))Check(text.Contains("다음 내 턴") && !SkillMechanics.Rules(card).Contains("턴 종료에 적용"),"tactical delayed text retains next-turn rather than turn-end timing: "+label);
            }
        }
    }
}
#endif
