using System;

namespace Lumia
{
    public enum RunMode { Normal = 0, Hard = 1, Endless = 2 }

    /// <summary>Shared mode labels, unlock rules and deterministic difficulty arithmetic.</summary>
    public static class RunModes
    {
        public const int EndlessBossEnergyLimit = 6;

        public static RunMode Normalize(RunMode mode)
        {
            return mode == RunMode.Hard || mode == RunMode.Endless ? mode : RunMode.Normal;
        }

        public static RunMode ModeFor(RunState state) { return Normalize(state == null ? RunMode.Normal : state.mode); }

        public static string Name(RunMode mode)
        {
            switch (Normalize(mode))
            {
                case RunMode.Hard: return "하드";
                case RunMode.Endless: return "무한";
                default: return "노말";
            }
        }

        public static string Description(RunMode mode)
        {
            switch (Normalize(mode))
            {
                case RunMode.Hard: return "적 최대 체력이 25%, 직접 공격 피해가 10% 증가합니다. 3막의 니아를 이기고 탈출하면 무한 모드가 열립니다.";
                case RunMode.Endless: return "하드 규칙으로 시작하며, 3막의 니아를 이기면 덱과 성장을 유지한 채 다음 3막 주기로 이어집니다. 반복마다 적 체력은 20%p, 직접 공격 피해는 5%p 증가하며, 두 번째 주기부터 적 레벨은 20입니다. 보스 처치로 얻는 최대 코스트는 총 +6까지이며 조우 보너스는 별도로 유지됩니다. 패배하거나 정산 후 종료할 때 원정이 끝납니다.";
                default: return "기존 전투와 보상 규칙으로 3막을 탐사합니다. 니아를 이기고 탈출하면 하드 모드가 열립니다.";
            }
        }

        public static bool IsUnlocked(ResearchProfile profile, RunMode mode)
        {
            switch (Normalize(mode))
            {
                case RunMode.Hard: return profile != null && (profile.normalCleared || profile.hardCleared);
                case RunMode.Endless: return profile != null && profile.hardCleared;
                default: return true;
            }
        }

        static int Percent(RunMode mode, int cycle, int start, int growth)
        {
            mode = Normalize(mode);
            if (mode == RunMode.Normal) return 100;
            long extra = mode == RunMode.Endless ? (long)Math.Max(0, cycle) * growth : 0;
            return (int)Math.Min(int.MaxValue, start + extra);
        }

        public static int EnemyHealthPercent(RunMode mode, int cycle = 0) { return Percent(mode, cycle, 125, 20); }
        public static int EnemyDamagePercent(RunMode mode, int cycle = 0) { return Percent(mode, cycle, 110, 5); }
        public static int EnemyHealthPercent(RunState state) { return EnemyHealthPercent(ModeFor(state), state == null ? 0 : state.endlessCycle); }
        public static int EnemyDamagePercent(RunState state) { return EnemyDamagePercent(ModeFor(state), state == null ? 0 : state.endlessCycle); }

        static int Scale(int value, int percent)
        {
            if (value <= 0) return 0;
            return (int)Math.Min(int.MaxValue, ((long)value * percent + 99) / 100);
        }

        public static int ScaleEnemyHealth(RunState state, int value) { return Scale(value, EnemyHealthPercent(state)); }
        public static int ScaleEnemyDirectDamage(RunState state, int value) { return Scale(value, EnemyDamagePercent(state)); }

        public static string CycleLabel(RunState state)
        {
            return ModeFor(state) == RunMode.Endless ? "무한 " + ((long)Math.Max(0, state.endlessCycle) + 1) + "주기" : Name(ModeFor(state));
        }
    }
}
