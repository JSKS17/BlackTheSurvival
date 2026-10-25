using System;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        Texture2D CombatSubjectPortrait(string subjectId)
        {
            if (subjectId == WickelineMechanics.EnemyId) return PixelArt.Portrait(subjectId);
            return researchStore != null && researchStore.UsesRareAppearance(subjectId)
                ? PixelArt.RarePortrait(subjectId) : PixelArt.Portrait(subjectId);
        }

        void DrawLumiChallengePanel()
        {
            var combat = Engine.State.combat;
            Rect rect = new Rect(440, 303, 399, 129);
            Box(rect, Ink, Gold);
            Txt(new Rect(rect.x + 12, rect.y + 10, 375, 25), "누적 피해 " + combat.lumiDamage + " / 획득 " + combat.lumiCredits + " CR", 18, Gold, true);
            int next = 40 - combat.lumiDamage % 40;
            Para(new Rect(rect.x + 15, rect.y + 49, 369, 62), "다음 20 CR까지 피해 " + next + "\n루미는 공격하지 않습니다. 3턴 종료 시 자동으로 떠납니다.", 13, Text);
        }

        void DrawLumiOffer()
        {
            Header("작은 손님, 루미", "전투를 마친 자리에서 루미를 발견했습니다.");
            Tex(new Rect(72, 151, 358, 400), PixelArt.Portrait("lumi"), ScaleMode.ScaleToFit);
            Box(new Rect(469, 153, 734, 352), Panel, Gold);
            Txt(new Rect(494, 177, 684, 42), "루미를 붙잡을까요?", 28, Gold);
            bool kiosk = Engine.LumiNearKiosk;
            Para(new Rect(494, 237, 684, 110), kiosk
                ? "키오스크 주변입니다. 루미와 3턴 동안 싸워 누적 피해 40마다 20크레딧을 얻습니다. 루미는 공격하지 않으며, 세 번째 턴이 끝나면 떠납니다."
                : "키오스크에서 떨어진 곳입니다. 루미를 붙잡으면 특별 상점을 이용할 수 있습니다. 망원 카메라, 전술 강화 모듈, 할인 오브젝트, 휴대용 키오스크를 판매합니다.", 18, Text);
            if (Btn(new Rect(494, 379, 684, 48), kiosk ? "붙잡기 / 3턴 크레딧 챌린지" : "붙잡기 / 루미 상점", true, Gold))
            { Act(Engine.CatchLumi, kiosk ? "combat.start" : "kiosk.enter"); return; }
            if (Btn(new Rect(494, 441, 684, 40), "그냥 보내기", false, Muted)) Act(Engine.SendLumiAway, "ui.cancel");
            Txt(new Rect(74, 606, 1110, 26), "발견한 기록은 선택과 관계없이 연구 보고서에 남습니다.", 13, Muted);
        }

        void DrawLumiShop()
        {
            Header("루미의 특별 상점", "이번 만남에서 각 상품을 한 번씩 구매할 수 있습니다. 추가 할인은 적용되지 않습니다.");
            bool rareVisit = ResearchEncounters.SubjectForEvent(Engine.State.chosenEventId) == "lumi";
            Tex(new Rect(38, 156, 242, 343), rareVisit ? PixelArt.RarePortrait("lumi") : PixelArt.Portrait("lumi"), ScaleMode.ScaleToFit);
            var obj = GameDatabase.Object(Engine.LumiObjectId);
            DrawLumiProduct(new Rect(311, 122, 439, 208), "telescope_camera", "망원 카메라", "앞으로 두 줄의 구역 정보를 확인합니다. 지도에서 사용하며 보안 콘솔과 같은 정찰 효과입니다.");
            DrawLumiProduct(new Rect(772, 122, 439, 208), "tactical_module", "전술 강화 모듈", "가방에서 사용해 보유한 전술 스킬(F) 카드 한 장에 전술 강화 효과를 추가합니다.");
            DrawLumiProduct(new Rect(311, 352, 439, 208), "object", obj == null ? "오브젝트" : obj.name,
                "정가에서 40크레딧 할인합니다. 할인 쿠폰 등 다른 할인은 중복 적용되지 않습니다.");
            DrawLumiProduct(new Rect(772, 352, 439, 208), "portable_kiosk", "휴대용 키오스크", "가방에서 사용하면 일반 키오스크를 열어 오브젝트와 음식을 구매할 수 있습니다.");
            if (Btn(new Rect(960, 625, 251, 48), "루미와 작별하기  >", true, Mint)) Act(Engine.LeaveLumiShop, "ui.cancel");
            Txt(new Rect(36, 635, 892, 27), "구매한 도구는 덱 / 장비 / 가방의 재료·음식 탭에서 사용할 수 있습니다.", 12, Muted);
        }

        void DrawLumiProduct(Rect rect, string id, string title, string description)
        {
            Box(rect, Panel, Line);
            string icon = id == "object" ? Engine.LumiObjectId : id;
            Tex(new Rect(rect.x + 16, rect.y + 18, 55, 55), PixelArt.SystemIcon(icon), ScaleMode.ScaleToFit);
            Txt(new Rect(rect.x + 88, rect.y + 20, rect.width - 108, 32), title, 21, id == "object" ? Gold : Mint);
            Para(new Rect(rect.x + 18, rect.y + 85, rect.width - 36, 60), description, 14, Text);
            bool sold = Engine.LumiItemSold(id);
            if (Btn(new Rect(rect.x + 18, rect.y + 157, rect.width - 36, 35), sold ? "품절" : Engine.LumiItemPrice(id) + " CR / 구매", false, Gold, Engine.CanBuyLumiItem(id)))
                Act(() => Engine.BuyLumiItem(id), "kiosk.purchase");
        }

        float DrawLumiUtilityBagRows()
        {
            var state = Engine.State;
            if (state.telescopicCameras <= 0 && state.portableKiosks <= 0) return 249;
            DrawLumiUtility(new Rect(80, 249, 549, 82), "telescope_camera", "망원 카메라", state.telescopicCameras,
                "지도에서 보안 콘솔처럼 구역 정보를 확인합니다.", Engine.CanUseTelescopicCamera, () =>
                { if (Act(Engine.UseTelescopicCamera, "map.travel")) inventory = false; });
            DrawLumiUtility(new Rect(651, 249, 549, 82), "portable_kiosk", "휴대용 키오스크", state.portableKiosks,
                "전투 밖에서 일반 키오스크를 엽니다.", Engine.CanUsePortableKiosk, () =>
                { if (Act(Engine.UsePortableKiosk, "kiosk.enter")) inventory = false; });
            return 343;
        }

        void DrawLumiUtility(Rect rect, string icon, string title, int count, string description, bool usable, Action use)
        {
            Box(rect, Panel, count > 0 ? Gold : Line);
            Tex(new Rect(rect.x + 11, rect.y + 17, 45, 45), PixelArt.SystemIcon(icon), ScaleMode.ScaleToFit);
            Txt(new Rect(rect.x + 67, rect.y + 11, 317, 25), title + " ×" + count, 16, count > 0 ? Gold : Muted);
            Para(new Rect(rect.x + 67, rect.y + 42, 345, 30), description, 11, Muted);
            if (Btn(new Rect(rect.xMax - 119, rect.y + 23, 105, 34), "사용", false, Mint, usable)) use();
        }
    }
}
