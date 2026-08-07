namespace NAN2026.Core
{
    // 준보스 행동 페이즈: 0=대기 1=추격 2=공격거리
    public static class MidBossLogic
    {
        public static int Phase(float dist, float aggroRange, float attackRange)
        {
            if (dist <= attackRange) return 2;
            if (dist <= aggroRange) return 1;
            return 0;
        }
        // 타격 구간 [a,b] 안인가 (통일 패링: 구간 접촉)
        public static bool InStrikeInterval(float elapsed, float duration, float a, float b)
        {
            if (duration <= 0f) return false;
            float p = elapsed / duration;
            return p >= a && p <= b;
        }

        // 공격 진행률이 타격 순간을 지났는가
        public static bool HitMomentPassed(float elapsed, float duration, float hitFrac)
        {
            if (duration <= 0f) return false;
            return elapsed / duration >= hitFrac;
        }
    }
}
