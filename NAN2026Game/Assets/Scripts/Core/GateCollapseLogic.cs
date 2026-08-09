namespace NAN2026.Core
{
    // 게이트 붕괴 연출 페이즈: 지연 -> 붕괴(틴트 소거) -> 유지(빛 점화) -> 복귀
    public static class GateCollapseLogic
    {
        const float EPS = 0.0001f;
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        static float D(float d) => d < EPS ? EPS : d;

        /// 대상이 이 구간(단)에 속하는가. 1·2단만 세고 보스 구역(y10)은 제외하기 위한 판정.
        public static bool InClearBand(float y, float minY, float maxY)
        {
            return y >= minY && y <= maxY;
        }

        /// 게이트를 열 시점인가. 이미 열었으면 다시 열지 않는다.
        public static bool ShouldOpen(int remaining, int collected, bool alreadyOpened)
        {
            if (alreadyOpened) return false;
            if (collected <= 0) return false;   // 애초에 셀 대상이 없으면 열지 않는다(오작동 방지)
            return remaining <= 0;
        }

        /// 폴링 시점인가. 매 프레임 전수 검사하지 않기 위한 간격 판정.
        public static bool TickDue(float elapsed, float interval)
        {
            return interval <= 0f || elapsed >= interval;
        }

        // 0=지연, 1=붕괴, 2=유지, 3=복귀완료
        public static int GetPhase(float t, float delay, float collapse, float hold)
        {
            if (t < delay) return 0;
            if (t < delay + collapse) return 1;
            if (t < delay + collapse + hold - EPS) return 2;
            return 3;
        }

        // 잠금벽 틴트 알파 1 -> 0 (붕괴 구간)
        public static float TintAlpha(float t, float delay, float collapse)
        {
            if (t <= delay) return 1f;
            return 1f - Clamp01((t - delay) / D(collapse));
        }

        // 개방부 조명 0 -> 1 (유지 구간)
        public static float LightFactor(float t, float delay, float collapse, float hold)
        {
            if (t <= delay + collapse + EPS) return 0f;
            return Clamp01((t - delay - collapse) / D(hold));
        }

        // 카메라가 게이트를 보는 구간 (복귀 전까지)
        public static bool PanActive(float t, float delay, float collapse, float hold)
            => t < delay + collapse + hold - EPS;
    }
}
