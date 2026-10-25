using System;
using UnityEngine;

namespace Lumia
{
    /// <summary>A hostile NPC encounter, kept separate from the experiment-subject catalog.</summary>
    public sealed partial class LumiaGame
    {
        static readonly Color WickelineTone = C("b7db79");

        void DrawWickelineVictoryScene(CombatState combat)
        {
            Header("위클라인 제압", "처치 보상이 지급되었습니다. 기술 연출이 끝나면 모닥불을 이용합니다.");
            Tex(new Rect(0, 80, W, 359), PixelArt.BattleBackground(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 80, W, 359), new Color(.02f, .04f, .07f, .2f));
            if (effects.UltimateFrame(reduceMotion) < 0 || effects.UltimateEnemy)
                effects.DrawActor(new Rect(129, 169, 214, 215), PixelArt.Portrait("hana"), false, reduceMotion);
            effects.DrawActor(new Rect(913, 169, 218, 216), PixelArt.Portrait(WickelineMechanics.EnemyId), true, reduceMotion);
            Txt(new Rect(873, 388, 292, 22), "위클라인 · 제압 완료", 14, WickelineTone, true);
            Txt(new Rect(886, 410, 266, 18), "0 / " + combat.enemyMaxHp, 12, Text, true);
        }

        void DrawWickelineCombatPanel()
        {
            Rect rect = new Rect(440, 303, 399, 129);
            Box(rect, Ink, WickelineTone);
            Txt(new Rect(rect.x + 12, rect.y + 7, 375, 23), "위클라인 · 제어 지속 -50%", 15, WickelineTone, true);
            Para(new Rect(rect.x + 13, rect.y + 35, 373, 58),
                Engine.EnemyWickelineStatus + "\n처치: VF 혈액 샘플 1 · " + WickelineMechanics.CreditReward + " CR · 나무/운석 1", 12, Text);
            if (Btn(new Rect(rect.x + 12, rect.y + 99, 375, 25), "위클라인 기술 · 대응 방법 +", false, WickelineTone))
                OpenBossGuide(WickelineMechanics.EnemyId);
        }

        void DrawWickelineGuide()
        {
            if (Engine == null) { inspectBoss = ""; return; }
            Modal(new Rect(210, 82, 860, 562));
            Tex(new Rect(237, 110, 80, 114), PixelArt.Portrait(WickelineMechanics.EnemyId), ScaleMode.ScaleToFit);
            Para(new Rect(333, 112, 708, 58), "위클라인 · 행동 예고와 대응", 24, WickelineTone);
            Txt(new Rect(333, 183, 708, 24), "특수 NPC / 처치 후 모닥불 이용", 14, Gold);
            string text = CachedNormalize(Engine.EnemyWickelineDescription);
            float height = Math.Max(294, CachedTextHeight(text, 16, 781) + 12);
            bossGuideScroll = BeginTouchScrollView("wickeline-guide", new Rect(237, 235, 804, 310), bossGuideScroll, new Rect(0, 0, 781, height));
            Para(new Rect(0, 0, 781, height), text, 16, Text);
            EndTouchScrollView();
            if (Btn(new Rect(237, 580, 804, 40), "위클라인 공략 닫기", true, WickelineTone)) inspectBoss = "";
        }

        void DrawWickelineSkillDetail()
        {
            var skill = WickelineMechanics.Card(inspectCard);
            if (skill == null) { inspectCard = ""; return; }
            Modal(new Rect(210, 82, 860, 562));
            Box(new Rect(237, 113, 215, 400), Panel, WickelineTone);
            Tex(new Rect(250, 177, 189, 220), PixelArt.Portrait(WickelineMechanics.EnemyId), ScaleMode.ScaleToFit);
            Txt(new Rect(250, 127, 189, 30), WickelineMechanics.Name, 22, WickelineTone, true);
            Txt(new Rect(250, 427, 189, 26), "특수 NPC 전용 행동", 14, Gold, true);
            Txt(new Rect(480, 111, 560, 39), skill.name, skill.name.Length > 17 ? 20 : 25, WickelineTone);
            Txt(new Rect(480, 163, 560, 28), "위클라인 행동 / 턴제 전투 규칙", 16, Gold);
            string text = skill.description ?? "";
            if (Engine != null && Engine.IsWickelineCombat)
            {
                var combat = Engine.State.combat;
                text = "현재 위클라인 / 체력 " + combat.enemyHp + "/" + combat.enemyMaxHp
                    + "\n다음 행동 / " + Engine.EnemyIntent + "\n\n" + text;
            }
            text = CachedNormalize(text);
            float height = Math.Max(300, CachedTextHeight(text, 16, 536) + 12);
            detailScroll = BeginTouchScrollView("wickeline-skill-detail", new Rect(480, 221, 559, 317), detailScroll, new Rect(0, 0, 536, height));
            Para(new Rect(0, 0, 536, height), text, 16, Text);
            EndTouchScrollView();
            if (Btn(new Rect(237, 580, 804, 40), "NPC 기술 설명 닫기", true, WickelineTone)) inspectCard = "";
        }
    }
}
