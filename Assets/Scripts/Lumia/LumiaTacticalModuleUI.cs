using System;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool tacticalModuleSelecting;
        bool inspectTacticalReward;
        RunState tacticalModuleState;
        int tacticalModuleSelected = -1, tacticalModuleRevision = -1;
        int[] tacticalModuleCandidates = new int[0];
        string tacticalModuleReference = "", tacticalModulePreviewText = "", tacticalModuleBonusText = "";
        Vector2 tacticalModuleListScroll, tacticalModuleDetailScroll;

        bool TacticalModuleChoiceOpen
        {
            get
            {
                if (tacticalModuleSelecting && (!inventory || lobby || Engine == null || !ReferenceEquals(tacticalModuleState, Engine.State)))
                    CloseTacticalModuleChoice();
                return tacticalModuleSelecting;
            }
        }

        float DrawTacticalModuleBagRow(float top = 249)
        {
            if (Engine.State.tacticalModules <= 0) return top + 7;
            Rect row = new Rect(80, top, 1120, 82);
            Box(row, Panel, Gold);
            Tex(new Rect(94, top + 15, 49, 49), PixelArt.SystemIcon("tactical_module"), ScaleMode.ScaleToFit);
            Txt(new Rect(159, top + 10, 713, 25), "전술 강화 모듈 ×" + Engine.State.tacticalModules, 17, Gold);
            bool usable = Engine.CanUseTacticalModule();
            Para(new Rect(159, top + 41, 782, 31), usable
                ? "보유한 F 카드 한 장을 선택해 스킬별 전술 강화 효과를 추가합니다."
                : Engine.TacticalModuleUnavailableReason(), 12, usable ? Text : Muted);
            if (Btn(new Rect(969, top + 22, 207, 37), "전술 카드 강화", false, Gold, usable))
            {
                tacticalModuleSelecting = true;
                tacticalModuleState = Engine.State;
                tacticalModuleRevision = -1;
                tacticalModuleSelected = -1;
                tacticalModuleReference = "";
                tacticalModuleListScroll = tacticalModuleDetailScroll = Vector2.zero;
            }
            return top + 94;
        }

        void CloseTacticalModuleChoice()
        {
            tacticalModuleSelecting = false;
            tacticalModuleState = null;
            tacticalModuleSelected = -1;
            tacticalModuleReference = "";
            tacticalModulePreviewText = tacticalModuleBonusText = "";
        }

        void RefreshTacticalModuleChoice()
        {
            int revision = EnsureDeckSnapshot().Revision;
            if (revision == tacticalModuleRevision) return;
            tacticalModuleRevision = revision;
            tacticalModuleCandidates = Engine.TacticalModuleCardIndices().ToArray();
            tacticalModuleSelected = -1;
            tacticalModuleReference = "";
            tacticalModulePreviewText = tacticalModuleBonusText = "";
        }

        void SelectTacticalModuleCard(int index)
        {
            if (tacticalModuleSelected == index)
            {
                tacticalModuleSelected = -1;
                tacticalModuleReference = "";
                tacticalModulePreviewText = tacticalModuleBonusText = "";
                return;
            }
            tacticalModuleSelected = index;
            tacticalModuleReference = Engine.State.deck[index];
            var original = Engine.PlayerCard(tacticalModuleReference);
            var preview = Engine.TacticalModulePreview(index);
            tacticalModuleBonusText = TacticalSkills.ModuleUpgradeDescription(original);
            tacticalModulePreviewText = preview == null ? "" : DescriptionSummary.Normalize(preview.description) + "\n\n" + CardPresentation.Rules(preview);
            tacticalModuleDetailScroll = Vector2.zero;
        }

        void DrawTacticalModuleChoice()
        {
            RefreshTacticalModuleChoice();
            Modal(new Rect(76, 91, 1128, 556));
            Tex(new Rect(98, 108, 43, 43), PixelArt.SystemIcon("tactical_module"), ScaleMode.ScaleToFit);
            Txt(new Rect(154, 111, 864, 30), "전술 강화 모듈 / F 카드 한 장 선택", 22, Gold);
            if (Btn(new Rect(1100, 108, 78, 32), "취소", false, Muted)) { CloseTacticalModuleChoice(); return; }
            Txt(new Rect(101, 158, 1068, 25), "같은 종류의 다른 카드에는 적용되지 않습니다. 확정할 때만 모듈 1개를 사용합니다.", 14, Muted);
            Txt(new Rect(101, 192, 514, 23), "강화 가능한 전술 카드 " + tacticalModuleCandidates.Length + "장", 15, Mint);
            var viewport = new Rect(101, 225, 520, 338);
            tacticalModuleListScroll = BeginTouchScrollView("tactical-module-cards", viewport, tacticalModuleListScroll,
                new Rect(0, 0, 500, Math.Max(338, (int)Math.Ceiling(tacticalModuleCandidates.Length / 2f) * 114)));
            var visible = CraftingCatalog.VisibleItems(tacticalModuleCandidates.Length, tacticalModuleListScroll.y, viewport.height, 2, 114);
            for (int i = visible.first; i < visible.end; i++)
            {
                int index = tacticalModuleCandidates[i];
                string reference = Engine.State.deck[index];
                var display = DisplayCard(reference);
                if (display == null) continue;
                Rect tile = new Rect(i % 2 * 251, i / 2 * 114, 241, 104);
                Box(tile, Panel, index == tacticalModuleSelected ? Gold : Line);
                Tex(new Rect(tile.x + 9, tile.y + 12, 35, 35), PixelArt.SkillIcon(display.card.id), ScaleMode.ScaleToFit);
                Txt(new Rect(tile.x + 54, tile.y + 9, 179, 24), display.card.name + display.suffix, 13, index == tacticalModuleSelected ? Gold : Mint);
                Txt(new Rect(tile.x + 54, tile.y + 34, 179, 19), "F · " + display.card.cost + "코스트 · 덱 " + (index + 1) + "번째", 11, Muted);
                Para(new Rect(tile.x + 10, tile.y + 60, 222, 35), CardDisplays.Preview(display), 11, Text);
                if (Hit(tile)) SelectTacticalModuleCard(index);
            }
            EndTouchScrollView();
            Rect previewArea = new Rect(645, 193, 534, 370);
            Box(previewArea, Panel, Line);
            bool selected = tacticalModuleSelected >= 0 && tacticalModuleSelected < Engine.State.deck.Count
                && Engine.State.deck[tacticalModuleSelected] == tacticalModuleReference
                && tacticalModuleCandidates.Contains(tacticalModuleSelected);
            if (selected)
            {
                Txt(new Rect(661, 206, 501, 26), PlayerCardName(tacticalModuleReference) + " [전술+]", 18, Gold);
                Para(new Rect(661, 244, 501, 56), tacticalModuleBonusText, 13, Mint);
                Txt(new Rect(661, 308, 501, 23), TacticalSkills.Definition(tacticalModuleReference)?.cobalt == true
                    ? "모듈 강화 후 전체 효과 · 코발트 강화 확장" : "모듈 강화 후 전체 효과", 13, Gold);
                float height = Math.Max(211, CachedTextHeight(tacticalModulePreviewText, 13, 478) + 10);
                tacticalModuleDetailScroll = BeginTouchScrollView("tactical-module-preview", new Rect(660, 338, 504, 211),
                    tacticalModuleDetailScroll, new Rect(0, 0, 480, height));
                Para(new Rect(0, 0, 478, height), tacticalModulePreviewText, 13, Text);
                EndTouchScrollView();
            }
            else Para(new Rect(668, 227, 485, 114), tacticalModuleCandidates.Length > 0
                ? "왼쪽에서 카드를 선택하면 추가되는 전술 강화 효과와 강화 후 전체 효과를 확인할 수 있습니다. 선택한 카드를 다시 누르면 선택을 취소합니다."
                : "강화할 F 카드가 없습니다. 취소하면 모듈은 그대로 보관됩니다.", 16, Muted);
            if (Btn(new Rect(101, 587, 520, 39), "취소하고 가방으로", false, Muted)) { CloseTacticalModuleChoice(); return; }
            if (Btn(new Rect(645, 587, 534, 39), "모듈 1개 사용 / 선택한 한 장 강화", true, Gold, selected && Engine.CanUseTacticalModule()))
                if (Act(() => Engine.UpgradeTacticalCard(tacticalModuleSelected), "ui.confirm")) CloseTacticalModuleChoice();
        }
    }
}
