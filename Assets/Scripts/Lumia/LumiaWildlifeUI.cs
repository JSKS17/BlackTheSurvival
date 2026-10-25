using System;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    /// <summary>Animal-only action previews never enter the player's card or reward catalog.</summary>
    public sealed partial class LumiaGame
    {
        static readonly Color MutationTone = C("c494ee");

        void DrawEnemyIntentIcon(Rect rect, string id)
        {
            if (WickelineMechanics.Card(id) != null)
            {
                Box(rect, Ink, WickelineTone);
                Tex(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), PixelArt.SystemIcon(WickelineMechanics.EnemyId), ScaleMode.ScaleToFit);
                return;
            }
            if (!WildlifeMechanics.IsAnimalSkill(id))
            {
                Tex(rect, PixelArt.SkillIcon(id), ScaleMode.ScaleToFit);
                return;
            }
            Box(rect, Ink, Gold);
            string animal = id == "wildlife_boar_charge" ? "boar" : "bear";
            var combat = Engine?.State.combat;
            bool mutant = combat != null && combat.animal == animal && combat.mutantAnimal;
            Tex(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), PixelArt.WildlifePortrait(animal, mutant), ScaleMode.ScaleToFit);
        }

        void DrawWildlifeVariantBadge()
        {
            var c = Engine?.State.combat;
            if (c == null || string.IsNullOrEmpty(c.animal) || !c.mutantAnimal) return;
            Rect r = new Rect(1098, 173, 66, 25);
            Box(r, Ink, MutationTone);
            Txt(r, "변이", 13, MutationTone, true);
        }

        void DrawWildlifeCombatPanel()
        {
            var c = Engine.State.combat;
            Rect r = new Rect(440, 303, 399, 129);
            Color tone = c.mutantAnimal ? MutationTone : Gold;
            Box(r, Ink, tone);
            string skillId = c.animal == "boar" ? "wildlife_boar_charge" : c.animal == "bear" ? "wildlife_bear_slam" : "";
            var skill = WildlifeMechanics.Card(skillId);
            var next = c.enemyPlan.Skip(c.enemyTurn ? c.enemyActionIndex : 0)
                .Select(Engine.EnemyActionCard).Where(card => card != null).Select(card => card.name).ToArray();
            string heading = next.Length == 0 ? "행동 마무리" : string.Join(" · ", next);
            Txt(new Rect(r.x + 12, r.y + 7, 375, 22), heading, 14, tone, true);
            string summary = "회피 0% · 오브젝트 " + WildlifeMechanics.ObjectDropChance(c.animal, c.mutantAnimal) + "% · 고기 " + GameEngine.WildlifeMeatDropChance + "%";
            if (c.mutantAnimal) summary += "\n변이: 체력 +" + (WildlifeMechanics.MutantHealthPercent - 100) + "% · 공격 +" + (WildlifeMechanics.MutantAttackPercent - 100) + "%";
            if (skill != null) summary += "\n" + (c.animal == "boar" ? "2턴마다 돌진 · 강한 제어·속박으로 중단" : "3턴마다 지면 강타 · 강한 제어로 중단");
            Para(new Rect(r.x + 13, r.y + 35, 373, skill == null ? 79 : 58), summary, 11, Text);
            if (skill != null && Btn(new Rect(r.x + 12, r.y + 99, 375, 25), "동물 전용 기술 · " + skill.name + " +", false, tone))
            {
                inspectCard = skill.id; inspectEnemyCard = true; summaryMode = false; detailScroll = Vector2.zero;
            }
        }

        void DrawWildlifeSkillDetail()
        {
            var skill = WildlifeMechanics.Card(inspectCard);
            if (skill == null) { inspectCard = ""; return; }
            string animal = skill.id == "wildlife_boar_charge" ? "boar" : "bear";
            Modal(new Rect(210, 82, 860, 562));
            Box(new Rect(237, 113, 215, 400), Panel, Gold);
            var combat = Engine?.State.combat;
            bool mutant = combat != null && combat.animal == animal && combat.mutantAnimal;
            Tex(new Rect(250, 177, 189, 220), PixelArt.WildlifePortrait(animal, mutant), ScaleMode.ScaleToFit);
            Txt(new Rect(250, 127, 189, 30), (mutant ? "변이 " : "") + WildlifeMechanics.Name(animal), 22, Gold, true);
            Txt(new Rect(250, 427, 189, 26), "야생동물 전용 기술", 14, Gold, true);
            Txt(new Rect(480, 111, 560, 39), skill.name, 25, Gold);
            Txt(new Rect(480, 163, 560, 28), "동물 행동 / " + (skill.movement ? "이동 기술" : "공격 기술"), 16, Gold);
            string text = "";
            if (Engine != null && Engine.State.stage == RunStage.Combat && Engine.State.combat.animal == animal)
            {
                var c = Engine.State.combat;
                text = "현재 야생동물 / " + c.enemyName + " · 체력 " + c.enemyHp + "/" + c.enemyMaxHp;
                if (c.mutantAnimal) text += "\n변이: 체력 +" + (WildlifeMechanics.MutantHealthPercent - 100) + "% · 공격 피해 +" + (WildlifeMechanics.MutantAttackPercent - 100) + "%";
                text += "\n오브젝트 드롭 " + WildlifeMechanics.ObjectDropChance(c.animal, c.mutantAnimal) + "% · 고기 " + GameEngine.WildlifeMeatDropChance + "%";
                text += "\n다음 행동 / " + Engine.EnemyIntent + "\n\n";
            }
            // The one canonical rule block ends with its debuff glossary.
            text += WildlifeMechanics.Description(skill.id);
            float height = Math.Max(300, CachedTextHeight(text, 16, 536) + 12);
            detailScroll = BeginTouchScrollView("wildlife-skill-detail", new Rect(480, 221, 559, 317), detailScroll, new Rect(0, 0, 536, height));
            Para(new Rect(0, 0, 536, height), text, 16, Text);
            EndTouchScrollView();
            if (Btn(new Rect(237, 580, 804, 40), "동물 기술 설명 닫기", true, Mint)) inspectCard = "";
        }
    }

    public sealed partial class CombatEffects
    {
        static readonly CombatVisualProfile BoarChargeProfile = new CombatVisualProfile("thrust", "dash", "D4AE82", "FFF0C5", .92f, .48f, 1, 187, false);
        static readonly CombatVisualProfile BearSlamProfile = new CombatVisualProfile("earth", "strike", "A998D2", "F0DBBC", .96f, .52f, 1, 226, false);

        static CombatVisualProfile WildlifeVisualProfile(string id)
        {
            if (id == "wildlife_boar_charge") return BoarChargeProfile;
            if (id == "wildlife_bear_slam") return BearSlamProfile;
            return null;
        }
    }
}
