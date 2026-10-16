using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    [DefaultExecutionOrder(10000)]
    public sealed class LumiaAdvancedCardVerification : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string result = "RUNNING", failure, version;
            public int screenshots, textMeasurements, allCardDefinitions, previewLayouts;
            public bool isolatedSavePath, allModalBoundsOnScreen;
            public bool fontOnly;
            public List<string> views = new List<string>();
        }
        readonly Result result = new Result();
        string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && (Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-advanced-cards-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-advanced-cards-font-verify") >= 0))
                new GameObject("BTS advanced card UI verification").AddComponent<LumiaAdvancedCardVerification>();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AdvancedCardsVerification"));
            Directory.CreateDirectory(output); WriteResult();
            var steps = Verify(); bool failed = false;
            while (!failed)
            {
                bool more = false; object next = null;
                try { more = steps.MoveNext(); if (more) next = steps.Current; }
                catch (Exception ex)
                {
                    failed = true; result.result = "FAIL"; result.failure = ex.ToString();
                    WriteResult(); Debug.LogException(ex);
                }
                if (!more || failed) break;
                yield return next;
            }
            if (failed) Application.Quit(1);
        }

        IEnumerator Verify()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null; yield return new WaitForSecondsRealtime(.4f);
            var game = LumiaGame.Instance;
            game.GuardAdvancedCardVerification(); result.isolatedSavePath = true;
            float styleDeadline=Time.realtimeSinceStartup+20f;
            while(!game.AdvancedCardVerificationStylesReady && Time.realtimeSinceStartup<styleDeadline)yield return null;
            if(!game.AdvancedCardVerificationStylesReady)throw new InvalidOperationException("The game did not render its first GUI frame before native verification.");
            result.version = Application.version;
            string previewAuditPath=Path.Combine(output,"preview-layout.json");
            game.ExportPreviewLayoutAudit(previewAuditPath);
            var previewAudit=JsonUtility.FromJson<LumiaGame.PreviewLayoutAudit>(File.ReadAllText(previewAuditPath));
            result.allCardDefinitions=previewAudit.cards;result.previewLayouts=previewAudit.measurements;
            result.textMeasurements = game.VerifyAdvancedCardText();
            result.allModalBoundsOnScreen = true;
            result.fontOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-bts-advanced-cards-font-verify")>=0;
            if(result.fontOnly)
            {
                result.result="PASS";WriteResult();Debug.Log("BTS ADVANCED CARD FONT PASS / text="+result.textMeasurements+" / isolatedSavePath=PASS / modalBounds=PASS");
                Application.Quit(0);yield break;
            }
            foreach (string view in new[] { "target", "charge", "foresight", "sacrifice", "ready_x_hand", "nina_link", "charged_preview", "summary_complex", "summary_catalog", "nina_guard_field" })
            {
                game.ShowAdvancedCardVerification(view);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, view + ".png"));
                yield return new WaitForSecondsRealtime(.15f);
                result.screenshots++; result.views.Add(view);
            }
            result.result = "PASS"; WriteResult();
            Debug.Log("BTS ADVANCED CARD UI PASS / screenshots=" + result.screenshots + " / text=" + result.textMeasurements + " / isolatedSavePath=PASS / modalBounds=PASS");
            Application.Quit(0);
        }
        void WriteResult() { File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(result, true)); }
    }

    public sealed partial class LumiaGame
    {
        bool AdvancedCardsVerificationEnabled => Debug.isDebugBuild && (Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-advanced-cards-verify") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-advanced-cards-font-verify") >= 0);
        public bool AdvancedCardVerificationStylesReady => initialized;

        public void GuardAdvancedCardVerification()
        {
            if (!AdvancedCardsVerificationEnabled || Path.GetFileName(savePath) != "lumia-verification.json")
                throw new InvalidOperationException("Advanced card UI verification requires its isolated save path.");
        }

        public int VerifyAdvancedCardText()
        {
            GuardAdvancedCardVerification(); int measurements = 0;
            var style = new GUIStyle { font=font,wordWrap=true,padding=new RectOffset(0,0,0,0),richText=false };
            Action<string, float, float, int, string> fits = (text, width, height, size, source) =>
            {
                font.RequestCharactersInTexture(text, size, FontStyle.Normal); style.fontSize = size;
                if (style.CalcHeight(new GUIContent(text), width) > height + .1f)
                    throw new InvalidOperationException("Advanced card text exceeds its UI bounds: " + source + " / " + text);
                measurements++;
            };
            string[] ids = { "sissela_e", "adina_r", "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r",
                "tazia_r", "echion_w", "eva_r", "sho_r", "sissela_r", "chiara_w", "lenox_q", "chloe_q", "chloe_w", "chloe_e", "chloe_r" };
            foreach (string id in ids)
            {
                var card = GameDatabase.Card(id);
                fits(DescriptionSummary.CardPreview(card, GameDatabase.Cards), 166, 109, 11, id + " hand summary");
                string summary = AdvancedCardMechanics.Summary(card);
                if (summary.Length > 0) fits(summary, 202, 143, 13, id + " choice summary");
                if (AdvancedCardMechanics.CanCharge(card)) fits(AdvancedCardMechanics.Description(card), 683, 133, 13, id + " full charge rules");
                fits(card.name + " ++", 144, 48, 13, id + " selection tile name");
            }
            foreach (string kind in new[] { "target", "charge", "foresight", "sacrifice" })
                fits(AdvancedChoicePrompt(kind), 938, 37, 14, kind + " prompt");
            fits("카드의 기본 효과를 지금 적용합니다.", 302, 90, 17, "charge immediate option");
            fits("코스트를 먼저 지불하고 다음 내 턴부터 한 번 무료로 발동합니다.", 302, 87, 17, "charge delayed option");
            fits("자신을 끌어당겨 방어도 12를 얻습니다.", 302, 90, 17, "friendly target option");
            fits("적을 끌어당겨 피해와 제어 효과를 줍니다.", 302, 90, 17, "hostile target option");
            foreach (var rect in new[] { new Rect(144, 66, 992, 588), new Rect(171, 190, 230, 327),
                new Rect(426, 223, 330, 72), new Rect(779, 223, 330, 72), new Rect(426, 390, 683, 128),
                new Rect(426, 225, 683, 293), new Rect(426, 581, 683, 43) })
                if (rect.x < 0 || rect.y < 0 || rect.xMax > W || rect.yMax > H)
                    throw new InvalidOperationException("Advanced card modal lies outside the game viewport.");
            return measurements;
        }

        public void ShowAdvancedCardVerification(string view)
        {
            GuardAdvancedCardVerification();
            tutorial = null; tutorialOffer = tutorialQueued = false;
            CloseStoryReplay(); CloseCombatInformation(); ResetAdvancedCardChoiceUI();
            lobby = settings = catalog = inventory = help = research = fieldInfo = enemyLoadout = false;
            modeSelecting = runMenu = endlessConfirm = retainSelecting = hyperloopSelecting = false;
            inspectCard = inspectTrait = inspectGear = inspectBoss = mapIntel = craftPending = "";
            effects.Clear(); nextPlayerActionAt = nextEnemyActionAt = 0; displayedTurn = -1;
            Engine = new TutorialSession().Engine; StoryScenes.Skip(Engine.State);
            var s = Engine.State; var c = s.combat;
            s.stage = RunStage.Combat; s.hp = 60; s.maxHp = 80; c.enemyHp = c.enemyMaxHp = 999;
            c.enemyTurn = false; c.energy = 5; c.turn = Math.Max(1, c.turn);
            c.hand = new List<string> { "sissela_e", "eleven_w", "adina_r", "tazia_r", "basic_attack", "nia_q", "basic_guard", "chiara_w" };
            c.drawPile = new List<string> { "eleven_q", "sissela_r", "eva_r" }; c.discardPile.Clear(); c.exhaustPile.Clear();
            s.deck = c.hand.Concat(c.drawPile).ToList();
            string choiceCard = view == "target" ? "sissela_e" : view == "charge" ? "eleven_w" : view == "foresight" ? "adina_r" : view == "sacrifice" ? "tazia_r" : "";
            if (choiceCard.Length > 0)
            {
                c.energy = Math.Max(c.energy, Engine.EffectiveCardCost(choiceCard));
                s.maxEnergyBonus = Math.Max(0, c.energy - GameEngine.StartingMaxEnergy);
                if (!Engine.BeginCardChoice(c.hand.IndexOf(choiceCard))) throw new InvalidOperationException("Native advanced choice could not open: " + view);
                displayedAdvancedChoice = Engine.PendingCardChoice;
                if (view == "foresight")
                {
                    advancedSwapHand = displayedAdvancedChoice.handReferences[0];
                    advancedSwapDraw = displayedAdvancedChoice.drawReferences[0];
                }
                if (view == "sacrifice")
                    foreach (string reference in displayedAdvancedChoice.handReferences.Take(2)) advancedSacrificeSelection.Add(reference);
                return;
            }
            if (view == "ready_x_hand" || view == "charged_preview")
            {
                c.hand = new List<string> { "eleven_w", "eleven_e", "eva_r", "sho_r", "chiara_w" };
                s.deck = c.hand.Concat(c.drawPile).ToList();
                c.chargedCards.Add(new ChargedCard { reference = "eleven_w", readyTurn = c.turn });
                c.chargedCards.Add(new ChargedCard { reference = "eleven_e", readyTurn = c.turn + 1 });
                if (view == "charged_preview") { combatInformationCard = "eleven_w"; informationHandIndex = 0; informationEnemy = false; }
            }
            if (view == "nina_link")
            {
                c.enemyId = "chloe"; c.enemyName = "클로에"; c.animal = null; c.enemyLevel = 6;
                CompanionMechanics.AfterCard(c.playerSkills, GameDatabase.Card("chloe_q"));
                CompanionMechanics.AfterCard(c.playerSkills, GameDatabase.Card("chloe_r"));
                CompanionMechanics.AfterCard(c.enemySkills, GameDatabase.Card("chloe_q"));
                CompanionMechanics.AfterCard(c.enemySkills, GameDatabase.Card("chloe_r"));
                c.hand = new List<string> { "chloe_q", "chloe_w", "chloe_e", "chloe_r", "basic_guard" }; s.deck = c.hand.Concat(c.drawPile).ToList();
            }
            if (view == "summary_complex")
            {
                c.hand = new List<string> { "cathy_q", "garnet_r", "isaac_e", "sua_w", "chloe_r" };
                s.deck = c.hand.Concat(c.drawPile).ToList();
            }
            if (view == "summary_catalog") { catalog=true; catalogQuery="클로에"; catalogPage=0; }
            if (view == "nina_guard_field")
            {
                CompanionMechanics.AfterCard(c.playerSkills, GameDatabase.Card("chloe_r"));
                fieldInfo=true;fieldEnemy=false;fieldScroll=Vector2.zero;
            }
        }
    }
}
