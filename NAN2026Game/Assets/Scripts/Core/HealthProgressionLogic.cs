namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 레벨업 Heal 증강처럼 "기준 최대체력 + 누적 보너스"를 상한(cap) 이내로 계산할 때 쓴다.
    public static class HealthProgressionLogic
    {
        /// 기준 최대체력(baseMaxHealth) + 누적 보너스(bonus)를 상한(cap) 이내로 계산한다.
        /// bonus가 음수라도 기준 최대체력 밑으로는 내려가지 않는다.
        public static int ClampedMaxHealth(int baseMaxHealth, int bonus, int cap)
        {
            int total = baseMaxHealth + bonus;
            if (total < baseMaxHealth) return baseMaxHealth;
            if (total > cap) return cap;
            return total;
        }

        /// 보너스를 amount만큼 늘렸을 때, 상한에 막혀 실제로 늘어난 최대체력 양을 계산한다.
        /// (이 값만큼 현재체력도 함께 회복시켜주면 "최대체력 증가 + 그만큼 즉시 회복"이 된다)
        public static int ActualMaxHealthGain(int baseMaxHealth, int bonusBefore, int amount, int cap)
        {
            if (amount <= 0) return 0;
            int before = ClampedMaxHealth(baseMaxHealth, bonusBefore, cap);
            int after = ClampedMaxHealth(baseMaxHealth, bonusBefore + amount, cap);
            return after - before;
        }
    }
}
