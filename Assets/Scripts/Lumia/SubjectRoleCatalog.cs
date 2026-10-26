using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Flags]
    public enum SubjectRole { None = 0, Fighter = 1, Marksman = 2, Tank = 4, Mage = 8, Assassin = 16, Support = 32 }

    // Official character roles and source evidence: docs/rune-roles-1.1.0/official-roles.json.
    // NPCs, including Lumi and Wickeline, deliberately have no subject role.
    public static class SubjectRoleCatalog
    {
        public const int RuneOfferMultiplier = 4;
        static readonly Dictionary<string, SubjectRole> Roles = new Dictionary<string, SubjectRole>(StringComparer.Ordinal)
        {
            // BEGIN OFFICIAL_ROLE_ENTRIES
            { "nia", SubjectRole.Mage }, // 니아
            { "jackie", SubjectRole.Fighter }, // 재키
            { "aya", SubjectRole.Mage | SubjectRole.Marksman }, // 아야
            { "hyunwoo", SubjectRole.Fighter }, // 현우
            { "yuki", SubjectRole.Fighter }, // 유키
            { "hyejin", SubjectRole.Mage }, // 혜진
            { "sua", SubjectRole.Fighter | SubjectRole.Mage }, // 수아
            { "isol", SubjectRole.Mage | SubjectRole.Marksman }, // 아이솔
            { "nadine", SubjectRole.Mage | SubjectRole.Marksman }, // 나딘
            { "emma", SubjectRole.Mage }, // 엠마
            { "nathapon", SubjectRole.Mage }, // 나타폰
            { "nicky", SubjectRole.Fighter }, // 니키
            { "daniel", SubjectRole.Assassin }, // 다니엘
            { "tia", SubjectRole.Mage }, // 띠아
            { "laura", SubjectRole.Fighter }, // 라우라
            { "lenox", SubjectRole.Tank }, // 레녹스
            { "leon", SubjectRole.Fighter }, // 레온
            { "rozzi", SubjectRole.Marksman }, // 로지
            { "luke", SubjectRole.Fighter }, // 루크
            { "dailin", SubjectRole.Fighter }, // 리 다이린
            { "rio", SubjectRole.Marksman }, // 리오
            { "martina", SubjectRole.Marksman }, // 마르티나
            { "mai", SubjectRole.Tank | SubjectRole.Support }, // 마이
            { "markus", SubjectRole.Tank | SubjectRole.Fighter }, // 마커스
            { "magnus", SubjectRole.Tank | SubjectRole.Fighter }, // 매그너스
            { "vanya", SubjectRole.Fighter }, // 바냐
            { "barbara", SubjectRole.Mage }, // 바바라
            { "bernice", SubjectRole.Marksman }, // 버니스
            { "bianca", SubjectRole.Mage }, // 비앙카
            { "celine", SubjectRole.Mage }, // 셀린
            { "sho", SubjectRole.Tank }, // 쇼우
            { "shoichi", SubjectRole.Assassin }, // 쇼이치
            { "sissela", SubjectRole.Mage }, // 시셀라
            { "silvia", SubjectRole.Fighter | SubjectRole.Mage }, // 실비아
            { "adela", SubjectRole.Mage }, // 아델라
            { "adriana", SubjectRole.Mage }, // 아드리아나
            { "adina", SubjectRole.Mage | SubjectRole.Support }, // 아디나
            { "isaac", SubjectRole.Fighter }, // 아이작
            { "alex", SubjectRole.Fighter }, // 알렉스
            { "jan", SubjectRole.Fighter }, // 얀
            { "estelle", SubjectRole.Tank | SubjectRole.Fighter }, // 에스텔
            { "aiden", SubjectRole.Fighter }, // 에이든
            { "echion", SubjectRole.Fighter }, // 에키온
            { "elena", SubjectRole.Tank }, // 엘레나
            { "johann", SubjectRole.Support }, // 요한
            { "william", SubjectRole.Marksman }, // 윌리엄
            { "irem", SubjectRole.Fighter | SubjectRole.Mage }, // 이렘
            { "eva", SubjectRole.Mage }, // 이바
            { "ian", SubjectRole.Fighter }, // 이안
            { "eleven", SubjectRole.Tank }, // 일레븐
            { "zahir", SubjectRole.Mage }, // 자히르
            { "jenny", SubjectRole.Marksman }, // 제니
            { "camilo", SubjectRole.Fighter }, // 카밀로
            { "karla", SubjectRole.Marksman }, // 칼라
            { "cathy", SubjectRole.Assassin }, // 캐시
            { "chloe", SubjectRole.Marksman }, // 클로에
            { "chiara", SubjectRole.Fighter }, // 키아라
            { "tazia", SubjectRole.Mage }, // 타지아
            { "theodore", SubjectRole.Marksman | SubjectRole.Support }, // 테오도르
            { "felix", SubjectRole.Fighter }, // 펠릭스
            { "priya", SubjectRole.Mage | SubjectRole.Support }, // 프리야
            { "fiora", SubjectRole.Fighter }, // 피오라
            { "piolo", SubjectRole.Fighter }, // 피올로
            { "hart", SubjectRole.Marksman }, // 하트
            { "haze", SubjectRole.Mage }, // 헤이즈
            { "debi_marlene", SubjectRole.Fighter }, // 데비&마를렌
            { "arda", SubjectRole.Mage | SubjectRole.Support }, // 아르다
            { "abigail", SubjectRole.Fighter }, // 아비게일
            { "alonso", SubjectRole.Tank }, // 알론소
            { "leni", SubjectRole.Support }, // 레니
            { "tsubame", SubjectRole.Marksman }, // 츠바메
            { "kenneth", SubjectRole.Fighter }, // 케네스
            { "katja", SubjectRole.Marksman }, // 카티야
            { "charlotte", SubjectRole.Support }, // 샬럿
            { "darko", SubjectRole.Fighter }, // 다르코
            { "lenore", SubjectRole.Mage }, // 르노어
            { "garnet", SubjectRole.Tank | SubjectRole.Fighter }, // 가넷
            { "yumin", SubjectRole.Mage }, // 유민
            { "hisui", SubjectRole.Fighter }, // 히스이
            { "justyna", SubjectRole.Mage }, // 유스티나
            { "istvan", SubjectRole.Fighter }, // 이슈트반
            { "xuelin", SubjectRole.Fighter | SubjectRole.Assassin }, // 슈린
            { "henry", SubjectRole.Mage }, // 헨리
            { "blair", SubjectRole.Fighter }, // 블레어
            { "mirka", SubjectRole.Tank | SubjectRole.Fighter }, // 미르카
            { "fenrir", SubjectRole.Fighter }, // 펜리르
            { "coraline", SubjectRole.Mage }, // 코렐라인
            { "bihyung", SubjectRole.Fighter }, // 비형
            { "craver", SubjectRole.Mage | SubjectRole.Marksman }, // 크레이버
            { "lucia", SubjectRole.Mage }, // 루치아
            { "ceres", SubjectRole.Tank | SubjectRole.Support }, // 세레스
            // END OFFICIAL_ROLE_ENTRIES
        };
        static readonly Dictionary<string, SubjectRole> OwnerRoles = GameDatabase.Characters.ToDictionary(
            character => character.name, character => ForSubject(character.id), StringComparer.Ordinal);

        public static SubjectRole ForSubject(string id)
        {
            SubjectRole value;
            return !string.IsNullOrEmpty(id) && Roles.TryGetValue(id, out value) ? value : SubjectRole.None;
        }

        public static SubjectRole ForOwner(string owner)
        {
            SubjectRole value;
            return !string.IsNullOrEmpty(owner) && OwnerRoles.TryGetValue(owner, out value) ? value : SubjectRole.None;
        }

        public static SubjectRole FavoredRoles(string tree)
        {
            switch (tree)
            {
                case "파괴": return SubjectRole.Fighter | SubjectRole.Marksman;
                case "저항": return SubjectRole.Tank | SubjectRole.Fighter;
                case "혼돈": return SubjectRole.Mage | SubjectRole.Assassin;
                case "지원": return SubjectRole.Tank | SubjectRole.Support;
                default: return SubjectRole.None;
            }
        }

        public static bool FavorsOwner(string tree, string owner) => (FavoredRoles(tree) & ForOwner(owner)) != SubjectRole.None;

        public static string TreeLabel(string tree)
        {
            switch (tree)
            {
                case "파괴": return "파괴";
                case "저항": return "저항";
                case "혼돈": return "혼돈";
                case "지원": return "지원";
                default: return "룬 미선택";
            }
        }

        public static string RoleLabel(string tree)
        {
            switch (tree)
            {
                case "파괴": return "전사·원거리 딜러";
                case "저항": return "탱커·전사";
                case "혼돈": return "스킬 딜러·암살자";
                case "지원": return "탱커·지원가";
                default: return "";
            }
        }

        public static string OfferDescription(string tree) => string.IsNullOrEmpty(RoleLabel(tree)) ? ""
            : TreeLabel(tree) + " · " + RoleLabel(tree) + " 실험체의 스킬·패시브 등장 가중치 " + RuneOfferMultiplier + "배";

        public static string OfferDetails(string tree) => OfferDescription(tree)
            + ". 4배는 후보 한 개의 추첨 가중치이며 확정 획득은 아닙니다. 시작 패시브·카드 선택, 리롤, 야생동물의 무작위 카드에 적용됩니다."
            + " 보유 패시브의 카드 보정 1.8배와 곱해져, 둘 다 해당하는 Q/W/E/R 카드는 7.2배가 됩니다. 두 역할에 모두 해당해도 역할군 보정은 4배만 적용됩니다."
            + " 실험체·보스 전투 및 지정 조우의 고정 보상은 바뀌지 않습니다. 보조 룬을 추가로 얻어도 역할군 가중치는 중첩되지 않습니다.";
    }
}
