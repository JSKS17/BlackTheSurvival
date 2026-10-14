using System;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        // Bonus values are percentage points added to the original 150% basic-hit multiplier.
        // Gear is looked up by saved ID, so older saves need no new serialized combat fields.
        public int CriticalDamagePercent { get { return CriticalMultiplier(EquipmentSum(g => g.critDamageBonus)); } }
        public int EnemyCriticalDamagePercent
        {
            get { return State.combat == null || !string.IsNullOrEmpty(State.combat.animal) ? 150 : CriticalMultiplier(EnemyEquipmentSum(g => g.critDamageBonus)); }
        }
        static int CriticalMultiplier(int bonus) { return (int)Math.Min(int.MaxValue, 150L + Math.Max(0, bonus)); }
        public int GearCriticalDamage(int damage, bool enemy = false)
        {
            return (int)Math.Min(int.MaxValue, Math.Max(0L, damage) * (enemy ? EnemyCriticalDamagePercent : CriticalDamagePercent) / 100L);
        }
    }
}
