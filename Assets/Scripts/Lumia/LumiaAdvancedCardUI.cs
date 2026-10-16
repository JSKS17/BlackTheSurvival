using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        AdvancedCardChoice displayedAdvancedChoice;
        readonly HashSet<string> advancedSacrificeSelection = new HashSet<string>(StringComparer.Ordinal);
        string advancedSwapHand = "", advancedSwapDraw = "";
        Vector2 advancedChoiceScroll;

        bool AdvancedCardChoiceOpen => !TutorialActive && Engine != null && Engine.PendingCardChoice != null;

        void DrawCompanionCombatLabel(Rect rect, SkillActorState actor, bool enemy)
        {
            var token = CompanionMechanics.Snapshot(actor).FirstOrDefault();
            if (token == null) return;
            string text = "니나 " + token.amount + "/" + token.cap + " · "
                + (token.remaining > 0 ? CompanionMechanics.LinkedSharePercent : CompanionMechanics.NormalSharePercent) + "% 분담"
                + (CompanionMechanics.HasLethalProtection(actor) ? " · 생존 1턴" : token.remaining > 0 ? " · 공유 " + token.remaining + "턴" : "");
            Box(rect, Ink, enemy ? Pink : Mint);
            Txt(new Rect(rect.x + 5, rect.y + 3, rect.width - 10, rect.height - 6), text, 10, enemy ? Pink : Mint, true);
        }

        void BeginAdvancedCardPlay(int handIndex)
        {
            if (Engine == null || AdvancedCardChoiceOpen) return;
            retainSelecting = false;
            if (Act(() => Engine.BeginCardChoice(handIndex)) && Engine.PendingCardChoice == null)
                nextPlayerActionAt = Time.unscaledTime + .18f;
        }

        void ResetAdvancedCardChoiceUI()
        {
            displayedAdvancedChoice = null;
            advancedSacrificeSelection.Clear();
            advancedSwapHand = advancedSwapDraw = "";
            advancedChoiceScroll = Vector2.zero;
        }

        void ResolveAdvancedCardChoice(string mode)
        {
            var choice = Engine?.PendingCardChoice;
            if (choice == null) return;
            List<string> selected = choice.kind == "sacrifice"
                ? choice.handReferences.Where(advancedSacrificeSelection.Contains).ToList()
                : choice.kind == "foresight" && advancedSwapHand.Length > 0 ? new List<string> { advancedSwapHand } : null;
            if (!Act(() => Engine.ResolveCardChoice(mode, selected, advancedSwapDraw))) return;
            ResetAdvancedCardChoiceUI();
            nextPlayerActionAt = Time.unscaledTime + .18f;
        }

        void CancelAdvancedCardChoice()
        {
            if (Engine != null && Act(Engine.CancelCardChoice, "ui.select")) ResetAdvancedCardChoiceUI();
        }

        void DrawAdvancedCardChoice()
        {
            var choice = Engine?.PendingCardChoice;
            if (choice == null) { ResetAdvancedCardChoiceUI(); return; }
            if (!ReferenceEquals(displayedAdvancedChoice, choice))
            {
                ResetAdvancedCardChoiceUI(); displayedAdvancedChoice = choice;
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { Event.current.Use(); CancelAdvancedCardChoice(); return; }
            var card = Engine.PlayerCard(choice.reference);
            if (card == null) { CancelAdvancedCardChoice(); return; }
            Modal(new Rect(144, 66, 992, 588));
            Txt(new Rect(171, 91, 938, 36), card.name + Engine.CardUpgradeSuffix(choice.reference) + "  /  " + AdvancedChoiceTitle(choice.kind), 24, Gold);
            Para(new Rect(171, 137, 938, 37), AdvancedChoicePrompt(choice.kind), 14, Text);
            DrawAdvancedChoiceCard(new Rect(171, 190, 230, 327), choice.reference);
            switch (choice.kind)
            {
                case "target": DrawAdvancedTargetChoice(card); break;
                case "charge": DrawAdvancedChargeChoice(card); break;
                case "foresight": DrawAdvancedForesightChoice(choice); break;
                case "sacrifice": DrawAdvancedSacrificeChoice(choice, card); break;
                default:
                    Para(new Rect(426, 211, 683, 80), "선택을 취소한 뒤 다시 카드를 선택해 주세요.", 16, Muted);
                    break;
            }
            if (Btn(new Rect(171, 581, 230, 43), "사용 취소  ·  Esc", false, Muted)) { CancelAdvancedCardChoice(); return; }
            Txt(new Rect(171, 536, 230, 30), "확정 전에는 코스트를 쓰지 않습니다.", 11, Muted, true);
        }

        static string AdvancedChoiceTitle(string kind)
        {
            switch (kind)
            {
                case "target": return "대상 선택";
                case "charge": return "충전 선택";
                case "foresight": return "미래의 카드 교환";
                case "sacrifice": return "소멸할 카드 선택";
                default: return "사용 방법 선택";
            }
        }

        static string AdvancedChoicePrompt(string kind)
        {
            switch (kind)
            {
                case "target": return "사용할 대상을 고르세요. 선택한 대상의 효과만 발동합니다.";
                case "charge": return "지금 사용하거나 코스트를 미리 지불해 충전하세요. 충전한 실제 카드 한 장은 손패에 남습니다.";
                case "foresight": return "손패 한 장과 위에서 뽑을 카드 세 장 중 한 장을 골라 교환하거나, 교환 없이 사용하세요.";
                case "sacrifice": return "다른 손패를 골라 이번 전투에서 소멸시키고 효과를 강화하세요. 선택한 카드를 다시 누르면 취소합니다.";
                default: return "사용 방법을 확인하고 확정하세요.";
            }
        }

        void DrawAdvancedChoiceCard(Rect rect, string reference)
        {
            var card = Engine.PlayerCard(reference); if (card == null) return;
            Box(rect, Panel, Gold);
            Tex(new Rect(rect.x + 75, rect.y + 18, 80, 80), PixelArt.SkillIcon(card.id), ScaleMode.ScaleToFit);
            Para(new Rect(rect.x + 14, rect.y + 112, rect.width - 28, 50), card.owner + " " + card.key + "  /  코스트 "
                + (AdvancedCardMechanics.IsX(card) ? "X" : Engine.EffectiveCardCost(reference).ToString()), 15, Gold);
            string text = AdvancedCardMechanics.Summary(card);
            if (string.IsNullOrEmpty(text)) text = CardSummary(card, true, reference);
            Para(new Rect(rect.x + 14, rect.y + 172, rect.width - 28, rect.height - 184), text, 13, Text);
        }

        void DrawAdvancedTargetChoice(CardDef card)
        {
            if (Btn(new Rect(426, 223, 330, 72), "자신에게 사용", true, Mint)) { ResolveAdvancedCardChoice("self"); return; }
            if (Btn(new Rect(779, 223, 330, 72), "적에게 사용", true, Pink)) { ResolveAdvancedCardChoice("enemy"); return; }
            Para(new Rect(440, 310, 302, 128), AdvancedCardMechanics.TargetChoiceDescription(card, "self"), 15, Mint);
            Para(new Rect(793, 310, 302, 128), AdvancedCardMechanics.TargetChoiceDescription(card, "enemy"), 15, Pink);
            Para(new Rect(426, 447, 683, 60), "자신을 지키는 효과와 적을 공격하는 효과는 동시에 발동하지 않습니다.", 14, Muted);
        }

        void DrawAdvancedChargeChoice(CardDef card)
        {
            if (Btn(new Rect(426, 223, 330, 72), "지금 사용", true, Mint)) { ResolveAdvancedCardChoice("now"); return; }
            if (Btn(new Rect(779, 223, 330, 72), "충전하기", true, Gold)) { ResolveAdvancedCardChoice("charge"); return; }
            Para(new Rect(440, 316, 302, 90), "카드의 기본 효과를 지금 적용합니다.", 17, Mint);
            Para(new Rect(793, 316, 302, 87), "코스트를 먼저 지불하고 다음 내 턴부터 한 번 무료로 발동합니다.", 17, Gold);
            Para(new Rect(426, 424, 683, 133), AdvancedCardMechanics.Description(card), 13, Muted);
        }

        void DrawAdvancedForesightChoice(AdvancedCardChoice choice)
        {
            Txt(new Rect(426, 190, 683, 23), "뽑을 카드 · 왼쪽부터 위쪽 순서", 13, Gold);
            for (int i = 0; i < choice.drawReferences.Count; i++)
            {
                string reference = choice.drawReferences[i];
                Rect rect = new Rect(426 + i * 232, 223, 219, 111);
                if (DrawAdvancedSelectionTile(rect, reference, advancedSwapDraw == reference, true, "위에서 " + (i + 1) + "번째"))
                    advancedSwapDraw = advancedSwapDraw == reference ? "" : reference;
            }
            if (choice.drawReferences.Count == 0) Txt(new Rect(426, 238, 683, 65), "뽑을 카드가 없습니다. 교환 없이 사용할 수 있습니다.", 15, Muted, true);
            Txt(new Rect(426, 357, 683, 23), "교환할 손패 · 사용 중인 카드는 제외됩니다", 13, Mint);
            int count = choice.handReferences.Count;
            advancedChoiceScroll = GUI.BeginScrollView(new Rect(426, 390, 683, 128), advancedChoiceScroll,
                new Rect(0, 0, Math.Max(661, count * 180), 108));
            for (int i = 0; i < count; i++)
            {
                string reference = choice.handReferences[i];
                if (DrawAdvancedSelectionTile(new Rect(i * 180, 0, 168, 108), reference, advancedSwapHand == reference, true))
                    advancedSwapHand = advancedSwapHand == reference ? "" : reference;
            }
            GUI.EndScrollView();
            if (Btn(new Rect(426, 581, 330, 43), "교환 없이 사용", false, Mint)) { ResolveAdvancedCardChoice("skip"); return; }
            if (Btn(new Rect(779, 581, 330, 43), "교환 후 사용", true, Gold, advancedSwapHand.Length > 0 && advancedSwapDraw.Length > 0))
                ResolveAdvancedCardChoice("swap");
        }

        void DrawAdvancedSacrificeChoice(AdvancedCardChoice choice, CardDef card)
        {
            int limit = AdvancedCardMechanics.SacrificeLimit(card);
            Txt(new Rect(426, 190, 683, 23), "소멸 선택 " + advancedSacrificeSelection.Count + " / " + limit + "  ·  덱에서는 제거되지 않습니다", 13, Gold);
            advancedChoiceScroll = GUI.BeginScrollView(new Rect(426, 225, 683, 293), advancedChoiceScroll,
                new Rect(0, 0, Math.Max(661, choice.handReferences.Count * 180), 286));
            for (int i = 0; i < choice.handReferences.Count; i++)
            {
                string reference = choice.handReferences[i]; bool selected = advancedSacrificeSelection.Contains(reference);
                Rect rect = new Rect(i * 180, 0, 168, 267);
                if (DrawAdvancedSelectionTile(rect, reference, selected, selected || advancedSacrificeSelection.Count < limit))
                {
                    if (selected) advancedSacrificeSelection.Remove(reference);
                    else advancedSacrificeSelection.Add(reference);
                }
            }
            GUI.EndScrollView();
            if (choice.handReferences.Count == 0) Txt(new Rect(426, 295, 683, 80), "선택할 다른 손패가 없습니다. 기본 효과로 사용할 수 있습니다.", 15, Muted, true);
            if (Btn(new Rect(426, 581, 683, 43), advancedSacrificeSelection.Count == 0 ? "소멸 없이 사용" : "선택한 " + advancedSacrificeSelection.Count + "장 소멸 후 사용", true, Gold))
                ResolveAdvancedCardChoice("cast");
            var bonus = AdvancedCardMechanics.ContextualBonus(card, 100, 100, 0, sacrificed: 1);
            string reward = bonus.damage > 0 ? "직접 피해 +" + bonus.damage : "방어도 +" + bonus.block;
            Para(new Rect(426, 533, 683, 33), "선택한 카드 1장당 " + reward + "입니다. 소멸한 카드는 이 전투가 끝나면 영구 덱으로 돌아옵니다.", 12, Muted);
        }

        bool DrawAdvancedSelectionTile(Rect rect, string reference, bool selected, bool enabled, string heading = "")
        {
            var card = Engine.PlayerCard(reference); if (card == null) return false;
            Box(rect, selected ? C("27433d") : Panel, selected ? Gold : enabled ? Line : C("253342"));
            float iconSize = rect.height > 150 ? 54 : 34;
            Tex(new Rect(rect.x + 12, rect.y + 12, iconSize, iconSize), PixelArt.SkillIcon(card.id), ScaleMode.ScaleToFit, enabled ? Color.white : Muted);
            string price = AdvancedCardMechanics.IsX(card) ? "X" : card.cost.ToString();
            Txt(new Rect(rect.x + iconSize + 22, rect.y + 12, rect.width - iconSize - 31, 21), card.key + " · " + price + "코스트", 11, Gold);
            Para(new Rect(rect.x + 12, rect.y + iconSize + 23, rect.width - 24, 48), card.name + Engine.CardUpgradeSuffix(reference), 13, selected ? Gold : Text);
            if (rect.height > 150) Para(new Rect(rect.x + 12, rect.y + iconSize + 82, rect.width - 24, rect.height-iconSize-94), CardSummary(card, false, reference), 11, Muted);
            if (heading.Length > 0) Txt(new Rect(rect.x + iconSize + 22, rect.y + 36, rect.width - iconSize - 31, 20), heading, 10, Muted);
            return Hit(rect, enabled);
        }
    }
}
