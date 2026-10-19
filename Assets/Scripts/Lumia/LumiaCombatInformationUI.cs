using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool combatHistoryOpen, informationEnemy, historyAllBattles;
        string combatInformationCard = "";
        int informationHandIndex = -1, informationFilter, informationSide, informationSerial = -1;
        Vector2 informationScroll, combatHistoryScroll;
        readonly HashSet<int> expandedCombatEntries = new HashSet<int>();
        CardUsePreview cachedCombatPreview;
        GameEngine informationEngine;
        CardUsePreview informationTextPreview;
        string informationTextUpgrade, informationText;
        GameEngine historyLayoutEngine;
        int historyLayoutSerial = -1, historyLayoutCount = -1, historyLayoutNode = -1;
        int historyLayoutFilter, historyLayoutSide, historyLayoutFont, historyExpansionRevision, historyLayoutExpansion = -1;
        bool historyLayoutAllBattles;
        CombatLogEntry[] historyRows = new CombatLogEntry[0];
        float[] historyRowHeights = new float[0], historyRowOffsets = new float[0];
        float historyContentHeight;
        bool CombatInformationOpen { get { return combatHistoryOpen || !string.IsNullOrEmpty(combatInformationCard); } }

        void CloseCombatInformation()
        {
            combatHistoryOpen = false; combatInformationCard = ""; informationHandIndex = -1;
            cachedCombatPreview = null; informationEngine = null; informationSerial = -1;
            informationScroll = combatHistoryScroll = Vector2.zero;
        }

        void DrawCardInformationButton(Rect cardRect, string id, int handIndex = -1, bool enemy = false)
        {
            // A separate header control keeps the card summary, classification and existing + intact.
            Rect r = new Rect(cardRect.xMax - 27, cardRect.y + 5, 21, 22);
            Box(r, Ink, Gold); Txt(r, "?", 14, Gold, true);
            if (Hit(r))
            {
                combatHistoryOpen = false; combatInformationCard = id; informationEnemy = enemy;
                informationHandIndex = handIndex; cachedCombatPreview = null; informationScroll = Vector2.zero;
            }
        }

        void DrawCombatInformationButton(Rect rect)
        {
            int count = Engine?.State.combatHistory?.Count ?? 0;
            if (Btn(rect, "전투 기록 +  " + count, false, Gold, Engine != null))
            {
                combatInformationCard = ""; combatHistoryOpen = true; combatHistoryScroll = Vector2.zero;
                informationFilter = informationSide = 0;
            }
        }

        void DrawCombatInformationOverlay()
        {
            if (Engine == null) { CloseCombatInformation(); return; }
            if (combatHistoryOpen) { DrawExpandedCombatHistory(); return; }
            if (string.IsNullOrEmpty(combatInformationCard)) return;
            if (cachedCombatPreview == null || informationEngine != Engine || informationSerial != Engine.State.combatHistorySerial)
            {
                cachedCombatPreview = Engine.PreviewCardUse(combatInformationCard, informationEnemy, informationHandIndex);
                informationEngine = Engine; informationSerial = Engine.State.combatHistorySerial;
            }
            var p = cachedCombatPreview;
            var display = informationEnemy ? null : DisplayCard(p.cardId);
            var card = informationEnemy ? Engine.EnemyActionCard(p.cardId) : display?.card;
            if (card == null) { CloseCombatInformation(); return; }
            string upgrade = p.enemy ? "" : display?.label ?? "";
            Modal(new Rect(180, 76, 920, 580));
            Tex(new Rect(209, 105, 56, 56), PixelArt.SkillIcon(card.id), ScaleMode.ScaleToFit);
            Txt(new Rect(281, 101, 779, 34), "사용 전 확인  /  " + p.name + (p.enemy ? "" : display?.suffix ?? ""), p.name.Length > 18 ? 18 : 24, string.IsNullOrEmpty(upgrade) ? Mint : Gold);
            Txt(new Rect(282, 142, 778, 25), card.owner + " " + card.key + " · " + p.classification + (p.enemy ? " · 상대의 현재 상태 기준" : ""), 13, Gold);
            Color available = p.canUse ? Mint : p.enemy ? Muted : Pink;
            Para(new Rect(209, 183, 851, 36), p.unavailableReason, 14, available);
            Box(new Rect(209, 224, 851, 52), Ink, Line);
            string price = "현재 코스트 " + (AdvancedCardMechanics.IsX(card) ? "X (" + p.currentCost + ")" : p.currentCost.ToString()) + " / 남은 " + p.energy + "    기본 " + (AdvancedCardMechanics.IsX(card) ? "X" : p.baseCost.ToString());
            if (p.costPenalty > 0) price += " · 상태이상 가산 +" + p.costPenalty;
            if (p.discount > 0) price += " · 할인 −" + p.discount;
            if (p.hasFreeCast) price += " · 무료 사용권 " + p.freeUses + "회";
            else if (p.currentCost == 0) price += " · 0코스트 사용 가능";
            Para(new Rect(221, 234, 827, 35), price, 14, Gold);

            if (!ReferenceEquals(informationTextPreview, p) || informationTextUpgrade != upgrade)
            {
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(upgrade)) lines.Add("강화 / " + upgrade + " · 이 카드 한 장에만 적용됩니다.");
                lines.Add("피해 예측 / 일반 적중 " + p.normalDamage + " · 최대 " + p.hits + "회 적중 · 상대 방어도 " + p.targetBlock + (p.guaranteedHit ? " · 필중 (명중률 감소·회피 무시)" : " · 명중률 " + p.accuracyChance + "% · 상대 회피율 " + p.evasionChance + "%") + (p.criticalChance > 0 ? " · 기본 공격 치명타 " + p.criticalChance + "%" : ""));
                lines.Add(p.damageNote);
                if (!string.IsNullOrEmpty(p.bossWarning)) lines.Add("보스 대응 / " + p.bossWarning);
                lines.Add("조건 확인 / 아래 수치는 이 카드를 사용하기 직전의 필드를 기준으로 합니다. 적중이나 후속 행동을 요구하는 효과는 사용 전 확정되지 않습니다.");
                if (p.conditions.Count == 0) lines.Add("추가 수치·직전 기술·적중 조건이 없습니다. 카드의 기본 효과가 적용됩니다.");
                foreach (var condition in p.conditions)
                {
                    string heading = "[" + condition.result + "]";
                    string conditionText = string.IsNullOrEmpty(condition.condition) ? "별도 조건 없음" : condition.condition;
                    lines.Add(heading + "\n" + conditionText + (string.IsNullOrEmpty(condition.current) ? "" : "\n현재 / " + condition.current) + "\n효과 / " + condition.effect);
                }
                informationText = DescriptionSummary.Normalize(string.Join("\n\n", lines));
                informationTextPreview = p; informationTextUpgrade = upgrade;
            }
            string text = informationText;
            float height = Math.Max(245, CachedTextHeight(text, 14, 815) + 15);
            informationScroll = BeginTouchScrollView("combat-information", new Rect(209, 291, 851, 250), informationScroll, new Rect(0, 0, 815, height));
            Para(new Rect(0, 0, 815, height), text, 14, Text);
            EndTouchScrollView();
            if (Btn(new Rect(209, 560, 410, 43), "확인 닫기", false, Mint)) { CloseCombatInformation(); return; }
            bool mayPlay = !p.enemy && p.canUse && CanPlayInformationCard(p.handIndex);
            if (Btn(new Rect(635, 560, 425, 43), p.enemy ? "상대 행동 정보" : p.canUse ? "이 카드 사용  ·  코스트 " + (AdvancedCardMechanics.IsX(card) ? "X (" + p.currentCost + ")" : p.currentCost.ToString()) : "현재 사용할 수 없음", true, Gold, mayPlay))
            {
                int index = p.handIndex; CloseCombatInformation(); PlayInformationCard(index);
            }
            Txt(new Rect(209, 617, 851, 20), "이 창을 열거나 닫아도 코스트·무료 사용권·전투 난수·필드는 바뀌지 않습니다.", 11, Muted, true);
        }

        void DrawExpandedCombatHistory()
        {
            Modal(new Rect(145, 76, 990, 580));
            Txt(new Rect(176, 102, 927, 35), "전투 기록", 25, Mint);
            Txt(new Rect(176, 144, 927, 23), "최신 기록부터 표시합니다. 실제 처리 결과를 저장하며 최근 " + CombatInformation.HistoryLimit + "개까지 이어하기에 남습니다.", 12, Muted);
            string[] filters = { "전체", "카드 사용", "효과·상태" };
            for (int i = 0; i < filters.Length; i++)
                if (Btn(new Rect(176 + i * 124, 180, 115, 32), filters[i], informationFilter == i, Mint)) { informationFilter = i; combatHistoryScroll = Vector2.zero; }
            if (Btn(new Rect(565, 180, 156, 32), informationSide == 0 ? "양측 보기" : informationSide == 1 ? "하나만 보기" : "상대만 보기", false, Pink)) { informationSide = (informationSide + 1) % 3; combatHistoryScroll = Vector2.zero; }
            if (Btn(new Rect(735, 180, 181, 32), historyAllBattles ? "원정 전체 전투" : "현재·마지막 전투", historyAllBattles, Gold)) { historyAllBattles = !historyAllBattles; combatHistoryScroll = Vector2.zero; }
            if (Btn(new Rect(930, 180, 172, 32), "상세 접기", false, Muted) && expandedCombatEntries.Count > 0)
            { expandedCombatEntries.Clear(); historyExpansionRevision++; }
            var history = Engine.State.combatHistory;
            int node = Engine.State.stage == RunStage.Combat ? Engine.State.activeNodeId : history?.LastOrDefault()?.nodeId ?? -1;
            EnsureCombatHistoryLayout(history, node);
            var rows = historyRows;
            combatHistoryScroll = BeginTouchScrollView("combat-history", new Rect(176, 228, 927, 330), combatHistoryScroll, new Rect(0, 0, 899, Math.Max(330, historyContentHeight)));
            for (int i = FirstCombatHistoryRow(combatHistoryScroll.y); i < rows.Length && historyRowOffsets[i] <= combatHistoryScroll.y + 330; i++)
            {
                var entry = rows[i]; float rowHeight = historyRowHeights[i], y = historyRowOffsets[i];
                Rect r = new Rect(0, y, 893, rowHeight);
                if (y + rowHeight >= combatHistoryScroll.y && y <= combatHistoryScroll.y + 330)
                {
                    Box(r, Ink, entry.enemy ? Pink : Mint);
                    string prefix = entry.act + "막 · " + entry.turn + "턴 · " + (entry.enemy ? entry.opponent : "하나");
                    Txt(new Rect(12, y + 8, 770, 23), prefix + " / " + entry.title, 13, entry.enemy ? Pink : Mint);
                    Para(new Rect(12, y + 35, 853, rowHeight - 40), entry.detail ?? "", 13, Text);
                    bool expanded = expandedCombatEntries.Contains(entry.sequence);
                    if (Btn(new Rect(810, y + 7, 70, 25), expanded ? "접기 −" : "상세 +", expanded, Gold))
                    { if (expanded) expandedCombatEntries.Remove(entry.sequence); else expandedCombatEntries.Add(entry.sequence); historyExpansionRevision++; }
                    if (expanded)
                    {
                        var display = entry.enemy ? null : DisplayCard(entry.cardId);
                        var card = entry.enemy ? Engine.EnemyActionCard(entry.cardId) : display?.card;
                        string origin = card == null ? "전투 흐름" : WildlifeMechanics.IsAnimalSkill(card.id) ? card.owner + " · 동물 전용 기술" : card.owner + " " + card.key + " · " + (display == null ? CardPresentation.ClassificationLabel(card) : display.classification);
                        string upgrade = entry.enemy ? "" : display?.label ?? "";
                        if (!string.IsNullOrEmpty(upgrade)) origin += " · 강화 / " + upgrade;
                        string metrics = "체력 피해 " + entry.damage + " / 방어 흡수 " + entry.absorbed + " / 명중 실패 " + entry.missed + " / 회피 " + entry.avoided + " / 방어 +" + entry.block + " / 회복 +" + entry.heal + (entry.paidCost >= 0 ? " / 지불 코스트 " + entry.paidCost : "");
                        Para(new Rect(12, y + rowHeight - 48, 853, 44), origin + "\n" + metrics, 11, Muted);
                    }
                }
            }
            if (rows.Length == 0) Para(new Rect(14, 15, 860, 50), "표시할 기록이 없습니다. 이전 버전에서 진행한 행동은 복원하지 않으며, 업데이트 후 발생한 전투 행동부터 기록합니다.", 14, Muted);
            EndTouchScrollView();
            if (Btn(new Rect(176, 581, 927, 43), "전투 기록 닫기", true, Mint)) CloseCombatInformation();
        }

        void EnsureCombatHistoryLayout(List<CombatLogEntry> history, int node)
        {
            int count = history?.Count ?? 0, serial = Engine.State.combatHistorySerial;
            if (ReferenceEquals(historyLayoutEngine, Engine) && historyLayoutSerial == serial && historyLayoutCount == count
                && historyLayoutNode == node && historyLayoutFilter == informationFilter && historyLayoutSide == informationSide
                && historyLayoutAllBattles == historyAllBattles && historyLayoutFont == craftingFontRevision
                && historyLayoutExpansion == historyExpansionRevision) return;
            var rows = new List<CombatLogEntry>();
            for (int i = count - 1; i >= 0; i--)
            {
                var entry = history[i];
                if (entry == null || !historyAllBattles && entry.nodeId != node || informationSide != 0 && entry.enemy != (informationSide == 2)) continue;
                if (informationFilter == 1 && !(entry.kind ?? "").StartsWith("card_", StringComparison.Ordinal)
                    || informationFilter == 2 && entry.kind != "effect" && entry.kind != "debuff") continue;
                rows.Add(entry);
            }
            historyRows = rows.ToArray(); historyRowHeights = new float[rows.Count]; historyRowOffsets = new float[rows.Count];
            historyContentHeight = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                historyRowOffsets[i] = historyContentHeight;
                historyRowHeights[i] = Math.Max(79, 48 + CachedTextHeight(rows[i].detail ?? "", 13, 853)
                    + (expandedCombatEntries.Contains(rows[i].sequence) ? 53 : 0));
                historyContentHeight += historyRowHeights[i] + 8;
            }
            historyLayoutEngine = Engine; historyLayoutSerial = serial; historyLayoutCount = count; historyLayoutNode = node;
            historyLayoutFilter = informationFilter; historyLayoutSide = informationSide; historyLayoutAllBattles = historyAllBattles;
            historyLayoutFont = craftingFontRevision; historyLayoutExpansion = historyExpansionRevision;
        }

        int FirstCombatHistoryRow(float scrollY)
        {
            int first = 0, end = historyRows.Length;
            while (first < end)
            {
                int middle = first + (end - first) / 2;
                if (historyRowOffsets[middle] + historyRowHeights[middle] < scrollY) first = middle + 1; else end = middle;
            }
            return first;
        }
    }
}
