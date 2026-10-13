using System;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        TutorialSession tutorial;
        GameEngine tutorialReturnEngine;
        bool tutorialReturnLobby, tutorialOffer, tutorialQueued;
        bool TutorialActive => tutorial != null;
        bool CanPlayInformationCard(int index) => TutorialActive ? tutorial.CanPlay(index) && !effects.Casting : Engine != null && Engine.CanPlayCard(index) && !effects.Casting && Time.unscaledTime >= nextPlayerActionAt;
        void PlayInformationCard(int index)
        {
            if (!CanPlayInformationCard(index)) return;
            if (TutorialActive) { tutorial.Play(index); ConsumeCombatActions(); }
            else { Act(() => Engine.PlayCard(index)); nextPlayerActionAt = Time.unscaledTime + .18f; }
        }
        string TutorialPreference => PresentationVerificationEnabled ? "bts.presentation.verify.tutorial" : "bts.tutorial.seen";
        void QueueNewRunTutorial() { tutorialQueued = PlayerPrefs.GetInt(TutorialPreference, 0) == 0; }
        void UpdateTutorialOffer()
        {
            if (tutorialQueued && !StoryVisible && Engine != null && Engine.State.stage == RunStage.Preparation && !lobby)
            { tutorialQueued = false; tutorialOffer = true; }
        }
        void StartTutorial()
        {
            if (TutorialActive) return;
            tutorialReturnEngine = Engine; tutorialReturnLobby = lobby;
            tutorial = new TutorialSession(); Engine = tutorial.Engine; lobby = false; tutorialOffer = false;
            fieldInfo = inventory = help = settings = catalog = research = false; inspectCard = inspectTrait = inspectGear = "";
            CloseCombatInformation(); effects.Clear(); displayedTurn = -1; nextEnemyActionAt = nextPlayerActionAt = 0;
            AudioCue("ui.select");
        }
        void FinishTutorial()
        {
            if (!TutorialActive) return;
            Engine.CombatActions.Clear(); Engine = tutorialReturnEngine; lobby = tutorialReturnLobby;
            tutorial = null; tutorialReturnEngine = null; fieldInfo = inventory = false; inspectCard = "";
            CloseCombatInformation(); effects.Clear(); displayedTurn = -1;
            PlayerPrefs.SetInt(TutorialPreference, 1); PlayerPrefs.Save();
            AudioCue("ui.select");
        }
        void SkipTutorialOffer()
        { tutorialOffer = false; PlayerPrefs.SetInt(TutorialPreference, 1); PlayerPrefs.Save(); }
        void DrawTutorialOffer()
        {
            Modal(new Rect(290, 180, 700, 345));
            Tex(new Rect(316, 230, 120, 136), PixelArt.Portrait("nia"), ScaleMode.ScaleToFit);
            Txt(new Rect(454, 213, 500, 31), "니아의 연습용 아케이드", 24, Pink);
            Para(new Rect(454, 266, 493, 116), "하나, 바로 시작할 거야?\n나랑 코스트랑 콤보부터 연습해 봐. 금방 끝나!", 20, Text);
            if (Btn(new Rect(323, 415, 300, 54), "니아에게 배우기", true, Mint)) StartTutorial();
            if (Btn(new Rect(641, 415, 315, 54), "바로 시작하기", false, Muted)) SkipTutorialOffer();
        }
        void UpdateTutorial()
        {
            if (!TutorialActive) return;
            ConsumeCombatActions();
            var c = Engine.State.combat;
            if (c.enemyTurn && !effects.Busy && Time.unscaledTime >= nextEnemyActionAt && !fieldInfo && !CombatInformationOpen && string.IsNullOrEmpty(inspectCard))
            {
                Engine.AdvanceEnemyAction(); ConsumeCombatActions(); nextEnemyActionAt = Time.unscaledTime + (reduceMotion ? .08f : .22f);
            }
            tutorial.ObserveTurn();
        }
        void DrawTutorial()
        {
            GUI.enabled = true; currentLayer = 0;
            topLayer = CombatInformationOpen || !string.IsNullOrEmpty(inspectCard) ? 6 : fieldInfo ? 5 : 0;
            GUI.enabled = topLayer == 0;
            Fill(new Rect(0, 0, W, H), Ink);
            Header("니아의 연습용 아케이드", "연습 전용 전투 · 진행 중인 원정과 연구 기록은 그대로 보존됩니다.");
            Tex(new Rect(0, 80, W, 359), PixelArt.BattleBackground(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 80, W, 359), new Color(.02f, .04f, .07f, .5f));
            Box(new Rect(24, 98, 1232, 139), Ink, Pink);
            Tex(new Rect(39, 111, 98, 112), PixelArt.Portrait("nia"), ScaleMode.ScaleToFit);
            Txt(new Rect(157, 111, 900, 23), "니아  /  " + ((int)tutorial.Step + 1) + " / " + TutorialSession.Dialogue.Length, 16, Pink);
            Para(new Rect(157, 145, 1069, 77), tutorial.Line, 19, Text);
            var c = Engine.State.combat;
            Txt(new Rect(30, 254, 320, 26), "하나 · 체력 " + Engine.State.hp + "/80 · 방어도 " + c.block, 16, Mint);
            Txt(new Rect(430, 254, 400, 32), "사용 가능 코스트 " + c.energy + " / " + Engine.MaxEnergy, 24, Gold, true);
            Txt(new Rect(935, 254, 300, 26), "연습용 닭 · 예정 피해 " + c.intentDamage, 16, Pink, true);
            Txt(new Rect(385, 295, 510, 23), "아케이드 블록 " + SkillMechanics.Resource(c.playerSkills, "니아", "blocks") + "/3 · VF 배터리 " + SkillMechanics.Resource(c.playerSkills, "니아", "battery") + "/4", 14, Mint, true);
            Tex(new Rect(202, 294, 128, 138), PixelArt.Portrait("hana"), ScaleMode.ScaleToFit);
            Tex(new Rect(946, 309, 100, 117), PixelArt.Portrait("chicken"), ScaleMode.ScaleToFit);
            Txt(new Rect(854, 421, 293, 18), "연습용 표적 · 쓰러뜨릴 필요 없음", 12, Muted, true);
            var hand = c.hand.ToArray();
            for (int i = 0; i < hand.Length; i++)
            {
                Rect card = new Rect(24 + i * 178, 485, 168, 212);
                bool allowed = tutorial.CanPlay(i) && !effects.Casting;
                DrawCard(card, hand[i], allowed, allowed, true);
                DrawCardInformationButton(card, hand[i], i);
                if (Hit(card, allowed)) { tutorial.Play(i); ConsumeCombatActions(); break; }
            }
            if (tutorial.Step == TutorialStep.Welcome && Btn(new Rect(460, 342, 360, 57), "연습 시작!", true, Mint)) tutorial.Start();
            if (tutorial.Step == TutorialStep.EndTurn && Btn(new Rect(1070, 485, 186, 58), "턴 종료  >", true, Gold, !effects.Casting)) { tutorial.EndTurn(); ConsumeCombatActions(); }
            if (tutorial.Step == TutorialStep.Field)
            {
                if (Btn(new Rect(1070, 485, 186, 48), "필드 확인", true, Mint)) { tutorial.OpenField(); fieldInfo = true; fieldEnemy = false; fieldScroll = Vector2.zero; }
                if (Btn(new Rect(1070, 544, 186, 48), "다음", false, Gold, tutorial.FieldSeen)) tutorial.FinishField();
            }
            if (tutorial.Step == TutorialStep.Complete && Btn(new Rect(460, 342, 360, 57), "준비 끝!", true, Mint)) { FinishTutorial(); return; }
            DrawCombatInformationButton(new Rect(1070, 611, 186, 38));
            if (Btn(new Rect(1070, 18, 186, 35), "연습 그만하기", false, Muted)) { FinishTutorial(); return; }
            effects.Draw(label, reduceMotion);
            if (fieldInfo) { currentLayer = 5; GUI.enabled = true; DrawFieldDetail(); }
            if (!string.IsNullOrEmpty(inspectCard)) { currentLayer = 6; GUI.enabled = true; DrawCardDetail(); }
            if (CombatInformationOpen) { currentLayer = 6; GUI.enabled = true; DrawCombatInformationOverlay(); }
            GUI.enabled = true;
        }
    }
}
