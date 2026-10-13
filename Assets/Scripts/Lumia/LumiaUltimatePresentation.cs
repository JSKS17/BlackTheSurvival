using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        GameEngine presentationEngine;
        CombatState presentationCombat;

        bool UltimateResultHold
        {
            get { return !lobby && Engine != null && Engine.State.combat != null && effects.UltimateBusy
                && (Engine.State.stage == RunStage.Rewards || Engine.State.stage == RunStage.Lost); }
        }
        bool CombatPresentationPaused
        {
            get { return CombatInformationOpen || modeSelecting || runMenu || endlessConfirm || settings || inventory || catalog || research || help
                || fieldInfo || enemyLoadout || !string.IsNullOrEmpty(inspectBoss) || !string.IsNullOrEmpty(inspectGear)
                || !string.IsNullOrEmpty(inspectCard) || !string.IsNullOrEmpty(inspectTrait); }
        }

        void SyncCombatPresentation()
        {
            var combat = Engine == null ? null : Engine.State.combat;
            if (presentationEngine != Engine || presentationCombat != combat)
            {
                effects.Clear(); presentationEngine = Engine; presentationCombat = combat;
            }
            if (lobby || combat == null || StoryVisible || TutorialActive
                || (Engine.State.stage != RunStage.Combat && !UltimateResultHold)) effects.Clear();
            effects.Pause(CombatPresentationPaused);
        }

        void DrawTerminalUltimateBattle()
        {
            // The engine has already resolved the hit. Finish its visual before exposing reward cards.
            bool enabled = GUI.enabled;
            GUI.enabled = false;
            try { DrawCombat(); }
            finally { GUI.enabled = enabled; }
        }
    }
}
