using System.Collections.Generic;
using G = Lumia.BossGoal;
using F = Lumia.BossEffect;

namespace Lumia
{
    // Authored turn-based challenges. Original names/motifs: docs/BOSS_MECHANICS.md.
    // Values are fixed fan-game balance, separate from the real-time game's skill numbers.
    public static partial class BossProfiles
    {
        public static readonly List<BossProfile> All = new List<BossProfile>();

        static BossProfiles()
        {
            Core(); Classic(); Recent();
        }

        public static BossProfile Get(string id) => All.Find(x => x.subjectId == id);

        static void Add(string id, string title, string overview, params BossPhaseDef[] phases)
        {
            All.Add(new BossProfile { subjectId = id, title = title, overview = overview, phases = phases });
        }

        static BossPhaseDef P(int phase, string name, string tell, string response, string signature,
            G goal, F effect, int amount, F secondary = F.None, int secondAmount = 0,
            string resource = null, string label = null, int cap = 3, string status = null, int interval = 0)
        {
            int damage = phase == 1 ? 18 : phase == 2 ? 26 : 30;
            int block = phase == 1 ? 8 : phase == 2 ? 12 : 14;
            bool prohibition = goal == G.NoHeavyHit || goal == G.NoDebuffSkill || goal == G.NoMovement;
            int target = goal == G.Damage ? damage : goal == G.Block ? block
                : goal == G.NoHeavyHit ? (phase == 1 ? 24 : 32)
                : goal == G.NoDebuffSkill || goal == G.NoMovement ? 2
                : goal == G.DebuffSkill || goal == G.Movement ? 1 : goal == G.CheapCards ? 3 : phase == 1 ? 2 : 3;
            return new BossPhaseDef {
                name = name, tell = tell, response = response, signatureCardId = signature,
                goal = goal, goalAmount = target,
                fallbackGoal = goal == G.Damage ? G.Block : G.Damage,
                fallbackAmount = prohibition ? 0 : goal == G.Damage ? block : damage,
                interval = interval > 0 ? interval : phase == 1 ? 3 : 2,
                effect = effect, effectAmount = amount, secondaryEffect = secondary, secondaryAmount = secondAmount,
                resourceKey = resource, resourceLabel = label, resourceCap = cap, statusKey = status
            };
        }

        static void Core()
        {
            Add("nia", "탈출구 없는 컨티뉴", "블록 퍼즐에서 싱크 연산으로 넘어가고, 마지막에는 게임월드의 입력 규칙을 바꿉니다.",
                P(1,"블록 퍼즐","아케이드 벽이 완성됩니다.","저렴한 행동을 이어 블록의 빈틈을 찾으세요.","nia_q",G.CheapCards,F.ResourcePrepare,2,F.Drain,1,resource:"blocks",label:"아케이드 블록",cap:3),
                P(2,"싱크 과부하","배터리에 싱크 폭발이 예약됩니다.","강한 압박이나 충분한 방어로 싱크를 끊으세요.","nia_w",G.Damage,F.DelayedStrike,14,F.ResourcePrepare,2,resource:"battery",label:"VF 배터리",cap:4),
                P(3,"파이널 컨티뉴","게임월드가 마지막 입력을 검사합니다.","서로 다른 카드 키를 조합해 최종 패턴을 푸세요.","nia_r",G.MixedKeys,F.Shield,16,F.Prepare,1));
            Add("jackie", "끝나지 않는 피의 축제", "출혈 표식을 준비한 뒤 전기톱의 지연 학살과 회복으로 추격합니다.",
                P(1,"붉은 추격","연참의 출혈 표식을 준비합니다.","기술을 연속으로 사용해 추격의 박자를 끊으세요.","jackie_q",G.Skill,F.ResourcePrepare,2,F.Strike,5,resource:"bleeding",label:"출혈 표식",cap:3),
                P(2,"전기톱 학살","전기톱의 마지막 베기가 다가옵니다.","방어를 모아 학살을 막거나 먼저 강하게 압박하세요.","jackie_r",G.Block,F.DelayedStrike,14,F.Heal,6));
            Add("aya", "사선 통제", "연발 준비와 엄호 사격으로 행동을 제한하고 공포탄 뒤의 사선을 노립니다.",
                P(1,"엄호 사격","연발 사선에 노출됩니다.","방어를 준비해 사격 자세를 무너뜨리세요.","aya_w",G.Block,F.Strike,10,F.ResourcePrepare,1,resource:"rapid",label:"연발 준비",cap:2),
                P(2,"공포탄 경계","공포탄 뒤에 후속 사격이 준비됩니다.","다른 카드 키를 섞어 예측한 사선을 벗어나세요.","aya_r",G.MixedKeys,F.Status,1,F.DelayedStrike,12,status:"fear"));
            Add("hyunwoo", "끝까지 버티는 주먹", "큰 한 방을 기다리는 허세는 가벼운 행동으로 흘리고, 후반의 핵펀치 충격파는 방어로 받아냅니다.",
                P(1,"허세의 빈틈","큰 한 방을 받아칠 허세를 세웁니다.","카드를 2장 이상 사용하고, 한 카드의 직접 피해를 24 미만으로 유지해 허세를 흘리세요.","hyunwoo_w",G.NoHeavyHit,F.Counter,5,F.Shield,8),
                P(2,"충격파 압박","핵펀치의 충격파가 차오릅니다.","방어를 충분히 쌓아 주먹의 위력을 흘리세요.","hyunwoo_r",G.Block,F.DelayedStrike,16,F.Status,1,status:"attack_down"));
            Add("yuki", "마지막 단추의 검", "단추를 정리할 틈을 벌고 검흔과 무장 해제로 검의 간격을 지배합니다.",
                P(1,"반듯한 옷매무새","단추와 방어를 재정비합니다.","짧은 행동을 이어 옷매무새를 정리할 틈을 빼앗으세요.","yuki_w",G.CheapCards,F.ResourcePrepare,2,F.Shield,6,resource:"cuff",label:"단추",cap:3),
                P(2,"지는 꽃의 검흔","검흔에 마지막 일격을 새깁니다.","서로 다른 카드 키로 검술의 간격을 흔드세요.","yuki_r",G.MixedKeys,F.DelayedStrike,14,F.Status,1,status:"disarm"));
            Add("hyejin", "세 번 겹치는 흉조", "흉조를 모아 행동을 예측하고 부적의 지연 타격과 명왕진을 연결합니다.",
                P(1,"흉조의 부적","도주 방향에 흉조를 겹칩니다.","서로 다른 카드 키로 예언의 틀을 벗어나세요.","hyejin_q",G.MixedKeys,F.Status,1,F.ResourcePrepare,2,resource:"omen",label:"흉조",cap:3,status:"fear"),
                P(2,"명왕의 궤도","부적의 궤도가 닫힙니다.","기술을 이어 써 명왕진이 완성되기 전에 끊으세요.","hyejin_r",G.Skill,F.DelayedStrike,12,F.Prepare,1));
            Add("sua", "다시 쓰는 결말", "파랑새의 보호와 책갈피를 이용해 익숙한 기술을 다시 기억합니다.",
                P(1,"파랑새의 페이지","보호하는 페이지를 펼칩니다.","서로 다른 카드 키로 보호의 문장을 바꾸세요.","sua_w",G.MixedKeys,F.Shield,10,F.Counter,3),
                P(2,"기억의 책갈피","읽었던 장면에 책갈피를 꽂습니다.","기본 공격으로 반복되는 이야기의 호흡을 끊으세요.","sua_r",G.Basic,F.Heal,6,F.ResourcePrepare,1,resource:"bookmark",label:"책갈피",cap:1));
            Add("isol", "퇴로의 폭발물", "짧은 설치 준비와 폭탄 공격 자원을 쌓아 다음 폭발을 강화합니다.",
                P(1,"배선 점검","셈텍스 배선을 연결합니다.","저렴한 행동을 나눠 기폭 준비를 방해하세요.","isol_q",G.CheapCards,F.DelayedStrike,12,F.Prepare,1),
                P(2,"지뢰망 완성","폭탄 공격 준비를 지뢰망에 연결합니다.","압박하거나 방어해 지뢰망의 완성을 저지하세요.","isol_r",G.Damage,F.ResourcePrepare,2,F.DelayedStrike,14,resource:"charge",label:"폭탄 공격 준비",cap:3));
            Add("nadine", "사냥꾼의 추적선", "조준을 유지하며 거리를 벌린 뒤 늑대와 덫의 추격을 겹칩니다.",
                P(1,"흔들리지 않는 조준","다음 화살의 조준을 고정합니다.","기본 공격을 이어 조준을 흔드세요.","nadine_q",G.Basic,F.ResourcePrepare,1,F.Evasion,10,resource:"aim",label:"조준",cap:2),
                P(2,"늑대의 추격","늑대와 덫이 퇴로를 좁힙니다.","방어를 모아 추격을 끊거나 사냥꾼을 압박하세요.","nadine_r",G.Block,F.DelayedStrike,14,F.Counter,4));
            Add("emma", "세 소품의 피날레", "폭죽 모자와 토끼 마술을 번갈아 사용해 같은 대응을 반복하기 어렵게 만듭니다.",
                P(1,"모자 속 배선","폭죽 모자를 숨깁니다.","다른 카드 키를 섞어 마술의 속임수를 밝히세요.","emma_w",G.MixedKeys,F.ResourcePrepare,1,F.DelayedStrike,8,resource:"hat",label:"모자 잔상",cap:1),
                P(2,"토끼의 피날레","마지막 마술의 대사를 지웁니다.","기술을 이어 사용해 피날레를 중단하세요.","emma_r",G.Skill,F.Status,1,F.Shield,8,status:"silence"));
        }

        static partial void Classic();
        static partial void Recent();
    }
}
