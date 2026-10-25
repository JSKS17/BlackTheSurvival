using System;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class WickelineBattleState
    {
        public int version = 1, gasUntilTurn, isolationUntilTurn, structureDamage, collapsedUntilTurn, skipEnemyTurn;
        public bool isolationPending;
    }

    /// <summary>Enemy-only NPC actions. Never registered in the playable subject/card catalog.</summary>
    public static class WickelineMechanics
    {
        public const string EnemyId = "wickeline", Name = "위클라인";
        public const string Tracking = "wickeline_tracking", Gas = "wickeline_gas", Triplet = "wickeline_triplet",
            Control = "wickeline_control", Isolation = "wickeline_isolation", Release = "wickeline_release", Collapse = "wickeline_collapse";
        public const int CreditReward = 100, StructureThreshold = 90;
        public const string Guide = "3막 모닥불 5곳 중 한 곳에 등장하는 특수 NPC입니다. 처치하면 같은 구역의 모닥불을 바로 이용합니다.\n"
            + "위클라인의 행동은 아래 턴제 규칙을 따릅니다.\n"
            + "실험 대상 추적: 이 구역에 들어온 하나를 추적해 전투를 시작합니다. 일반 실험체와 카드 획득 목록에는 포함되지 않습니다.\n"
            + "5턴 주기: 신경가스와 기본 공격 → 트리플렛 코드 → 통제와 기본 공격 2회 → 격리 충전 → 격리 방출. 가스가 남은 트리플렛 턴에는 기본 공격 1회가 추가됩니다.\n"
            + "가스: 다음 두 플레이어 턴 동안 하나의 회복량 -40%. 격리: 다음 플레이어 턴까지 받는 체력 피해 -40%, 체력 20 + 잃은 체력의 10% 회복(치유 감소 적용). 방출은 넉백을 부여합니다.\n"
            + "불안정 구조: 받은 실제 체력 피해 90마다 최대 체력의 10%를 추가로 잃고 구조가 붕괴합니다. 그때 충전 중인 격리는 취소되고, 다음 상대 행동을 모두 건너뛰며 다음 플레이어 턴까지 받는 체력 피해 +30%. 붕괴 중 피해는 새 게이지에 누적되지 않습니다. 상시 군중 제어 지속 시간 -50%(최소 1턴).\n"
            + "확정 보상: VF 혈액샘플 1개 + 100 크레딧 + 생명의 나무 또는 운석 1개. 일반 카드·패시브·보스 코스트 보상은 없습니다.";

        static CardDef Action(string id, string name, string key, string description, int damage = 0, int hits = 1, bool movement = false)
        {
            return new CardDef { id=id, owner=Name, name=name, key=key, category="wickeline", cost=0,
                damage=damage, hits=hits, movement=movement, effect="poison", effectColor="#97E891", description=description };
        }
        static readonly CardDef[] cards = {
            Action(Tracking,"실험 대상 추적","추적","구역에 진입한 하나를 추적해 전투를 시작합니다. 이동 거리는 턴제 지도 진입으로 환산하며 별도 공격 피해나 치유 감소는 없습니다."),
            Action(Gas,"신경가스 살포","가스","다음 두 플레이어 턴까지 하나의 회복량을 40% 줄입니다. 가스가 남은 트리플렛 코드 턴에는 기본 공격 1회가 추가됩니다. 공격 속도·이동 속도 증가는 이 추가 행동으로 환산합니다."),
            Action(Triplet,"트리플렛 코드","주사기","세 갈래 주사기를 턴제 3회 타격으로 환산합니다. 각 타격은 별도로 명중·회피를 판정하고, 적중하면 하나에게 둔화 1턴을 부여합니다.",2,3),
            Action(Control,"통제","돌진","대상에게 접근하며 공격 속도가 상승하는 기술입니다. 턴제에서는 기본 공격 2회로 이어집니다. 속박으로 접근이 막히거나 강한 제어로 중단되면 두 공격 모두 취소됩니다.",0,1,true),
            Action(Isolation,"「격리」 충전","충전","체력 20 + 잃은 체력의 10%를 회복하며 치유 감소가 적용됩니다. 다음 플레이어 턴까지 받는 체력 피해가 40% 감소하고, 다음 상대 턴에 방출합니다. 구조 붕괴로 충전과 방출을 취소할 수 있습니다."),
            Action(Release,"「격리」 방출","방출","충전을 완료한 VF 에너지를 방출하여 피해를 입히고, 적중하면 하나에게 넉백 1턴을 부여합니다. 구조 붕괴로 충전이 취소되었다면 방출하지 않습니다.",16),
            Action(Collapse,"불안정 구조","붕괴","실제 체력 피해 90마다 최대 체력의 10%를 추가로 잃고 다음 상대 행동을 모두 건너뜁니다. 다음 플레이어 턴까지 받는 체력 피해가 30% 증가하며 충전 중인 격리는 취소됩니다. 붕괴 중에는 게이지를 쌓지 않습니다.")
        };
        public static CardDef Card(string id) { return cards.FirstOrDefault(x=>x.id==id); }
        public static bool IsAction(string id) { return Card(id)!=null; }
        public static bool GasActive(CombatState c) { return c?.wickeline!=null && c.wickeline.gasUntilTurn>=c.turn && c.wickeline.gasUntilTurn>0; }
        public static bool Isolating(CombatState c) { return c?.wickeline!=null && c.wickeline.isolationUntilTurn>=c.turn && c.wickeline.isolationPending; }
        public static bool Collapsed(CombatState c) { return c?.wickeline!=null && c.wickeline.collapsedUntilTurn>=c.turn && c.wickeline.collapsedUntilTurn>0; }
        public static int IncomingDamage(CombatState c,int damage)
        {
            if(c?.wickeline==null)return damage;
            if(Isolating(c))damage=damage*60/100;
            if(Collapsed(c))damage=damage*130/100;
            return Math.Max(0,damage);
        }
    }
}
