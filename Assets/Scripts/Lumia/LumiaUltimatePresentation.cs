using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        GameEngine presentationEngine;
        CombatState presentationCombat;

        bool UltimateResultHold
        {
            get { return !lobby && Engine != null && effects.UltimateBusy
                && (Engine.State.combat != null && (Engine.State.stage == RunStage.Rewards || Engine.State.stage == RunStage.Lost)
                    || WickelineVictoryPresentation); }
        }
        bool WickelineVictoryPresentation => Engine != null && presentationEngine == Engine && Engine.State.stage == RunStage.Campfire
            && Engine.State.combat == null && presentationCombat?.enemyId == WickelineMechanics.EnemyId;
        bool CombatPresentationPaused
        {
            get { return CombatInformationOpen || modeSelecting || runMenu || endlessConfirm || settings || inventory || catalog || research || help
                || fieldInfo || enemyLoadout || !string.IsNullOrEmpty(inspectBoss) || !string.IsNullOrEmpty(inspectGear)
                || !string.IsNullOrEmpty(inspectCard) || !string.IsNullOrEmpty(inspectTrait); }
        }

        void SyncCombatPresentation()
        {
            var combat = Engine == null ? null : Engine.State.combat;
            // Rewards and the camp are already committed. Keep only the detached visual state
            // until the fatal R finishes; never restore it to the engine or replay the reward.
            bool wickelineFinishing = WickelineVictoryPresentation && (effects.UltimateBusy || Engine.CombatActions.Any(action =>
                string.IsNullOrEmpty(action.kind) && string.IsNullOrEmpty(action.traitId)
                && UltimateAnimations.For(GameDatabase.Card(action.cardId)) != null));
            if (presentationEngine != Engine || presentationCombat != combat)
            {
                if (!wickelineFinishing) { effects.Clear(); presentationEngine = Engine; presentationCombat = combat; }
            }
            if (lobby || combat == null && !wickelineFinishing || StoryVisible || TutorialActive
                || (Engine.State.stage != RunStage.Combat && !UltimateResultHold && !wickelineFinishing)) effects.Clear();
            effects.Pause(CombatPresentationPaused);
        }

        void DrawTerminalUltimateBattle()
        {
            // The engine has already resolved the hit. Finish its visual before exposing reward cards.
            bool enabled = GUI.enabled;
            GUI.enabled = false;
            try { if (WickelineVictoryPresentation) DrawWickelineVictoryScene(presentationCombat); else DrawCombat(); }
            finally { GUI.enabled = enabled; }
        }
    }
}
