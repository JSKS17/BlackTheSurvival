using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        /// <summary>Isolated development-build fixture for the layered-story release's critical UI audit.</summary>
        public void VerificationCriticalInformation(string view)
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-layered-story-verify") < 0)
                throw new InvalidOperationException("Critical verification requires the isolated development verification launch.");
            CloseStoryReplay(); CloseStoryLayers();
            VerificationView("critical_detail"); CloseCombatInformation(); CloseStoryReplay();
            tutorialOffer = false;
            StoryScenes.Skip(Engine.State);
            var state = Engine.State; var combat = state.combat;
            state.mode = view.Contains("normal") ? RunMode.Normal : view.Contains("hard") ? RunMode.Hard : RunMode.Endless;
            state.gear = Enumerable.Repeat("light_insignia", 20).ToList();
            if (state.mode == RunMode.Endless) state.gearSlotBonuses[(int)GearSlot.Head] = 18;
            state.hp = state.maxHp = 5000;
            combat.enemyId = "aya"; combat.enemyName = "아야"; combat.animal = null;
            combat.enemyHp = combat.enemyMaxHp = 10000; combat.enemyBlock = combat.enemyEvasion = 0;
            combat.enemyGear = Enumerable.Repeat("light_insignia", 20).ToList();
            combat.playerTraits = new TraitActorState(); combat.enemyTraits = new TraitActorState();
            state.persistentTraits = combat.playerTraits;
            combat.playerStatuses = new StatusActorState(); combat.enemyStatuses = new StatusActorState();
            combat.hand = new List<string> { "basic_attack", "nia_q", "basic_guard" }; combat.energy = 99;
            if (view.EndsWith("_field", StringComparison.Ordinal))
            { inspectCard = ""; fieldInfo = true; fieldEnemy = false; fieldScroll = Vector2.zero; }
            else if (view.EndsWith("_preview", StringComparison.Ordinal))
            { inspectCard = ""; combatInformationCard = "basic_attack"; informationHandIndex = 0; informationEnemy = false; }
            else if (view.EndsWith("_gear", StringComparison.Ordinal))
            { inspectCard = ""; inspectGear = "light_insignia"; summaryMode = false; detailScroll = Vector2.zero; }
            else if (view.EndsWith("_help", StringComparison.Ordinal))
            { inspectCard = ""; help = true; helpScroll = new Vector2(0, 100); }
            else if (view.EndsWith("_hit", StringComparison.Ordinal))
            {
                inspectCard = ""; Engine.CombatActions.Clear();
                if (!Engine.PlayCard(0)) throw new InvalidOperationException("Critical verification could not play its basic attack.");
                var action = Engine.CombatActions.Last(x => x.cardId == "basic_attack" && string.IsNullOrEmpty(x.kind));
                if (!action.critical || Engine.CritChance != 100) throw new InvalidOperationException("Endless fixture did not critically hit at 100 percent.");
                ConsumeCombatActions(); effects.FreezeForVerification(.55f);
            }
            else { inspectCard = "basic_attack"; inspectEnemyCard = false; summaryMode = false; detailScroll = new Vector2(0, 110); }
            int expected = state.mode == RunMode.Endless ? 100 : 30;
            if (Engine.CritChance != expected || Engine.EnemyCritChance != expected)
                throw new InvalidOperationException("Native critical cap mismatch: " + view);
            Debug.Log("BTS CRITICAL UI " + view + " cap=" + Engine.DynamicCriticalChanceCap + " player=" + Engine.CritChance + " enemy=" + Engine.EnemyCritChance);
        }
    }
}
