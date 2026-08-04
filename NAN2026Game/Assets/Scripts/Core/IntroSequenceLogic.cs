namespace NAN2026.Core
{
    // 인트로 연출 페이즈 계산 (암전 -> 촛불 점화 -> 전역 확장 -> 완료)
    public static class IntroSequenceLogic
    {
        const float EPS = 0.0001f; // 프레임 시간 부동소수 경계 보호

        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        static float SafeDur(float d) => d < 0.0001f ? 0.0001f : d;

        // 0=암전, 1=점화, 2=확장, 3=완료
        public static int GetPhase(float t, float black, float ignite, float expand)
        {
            if (t < black) return 0;
            if (t < black + ignite) return 1;
            if (t < black + ignite + expand - EPS) return 2;
            return 3;
        }

        // 촛불 밝기 계수 0..1 (점화 페이즈에서 상승, 이후 유지)
        public static float CandleFactor(float t, float black, float ignite)
        {
            if (t <= black) return 0f;
            return Clamp01((t - black) / SafeDur(ignite));
        }

        // 전역 조명 계수 0..1 (확장 페이즈에서 상승)
        public static float GlobalFactor(float t, float black, float ignite, float expand)
        {
            if (t <= black + ignite) return 0f;
            return Clamp01((t - black - ignite) / SafeDur(expand));
        }

        // 플레이어 범위까지 밝아진 순간(확장 완료)부터 BGM 재생
        public static bool BgmShouldPlay(float t, float black, float ignite, float expand)
            => t >= black + ignite + expand - EPS;

        public static float TotalDuration(float black, float ignite, float expand)
            => black + ignite + expand;
    }
}
