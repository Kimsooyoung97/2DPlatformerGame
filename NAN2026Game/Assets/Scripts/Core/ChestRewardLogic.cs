namespace NAN2026.Core
{
    // 상자 보상 아이콘: 떠오름 → 플레이어에게 흡수 → 소멸.
    // 엔진 참조 없는 순수 로직 (NAN2026.Core, noEngineReferences)
    public static class ChestRewardLogic
    {
        public const int PhaseRise = 0;
        public const int PhaseAbsorb = 1;
        public const int PhaseDone = 2;

        public static int Phase(float elapsed, float riseTime, float absorbTime)
        {
            if (elapsed < riseTime) return PhaseRise;
            if (elapsed < riseTime + absorbTime) return PhaseAbsorb;
            return PhaseDone;
        }

        public static float EaseOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float inv = 1f - t;
            return 1f - inv * inv;
        }

        public static float EaseIn(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t * t;
        }

        // 상승 구간의 위쪽 오프셋(월드 유닛)
        public static float RiseOffset(float elapsed, float riseTime, float riseDistance)
        {
            if (riseTime <= 0f) return riseDistance;
            return riseDistance * EaseOut(elapsed / riseTime);
        }

        // 흡수 진행도 0~1
        public static float AbsorbT(float elapsed, float riseTime, float absorbTime)
        {
            if (absorbTime <= 0f) return 1f;
            float t = (elapsed - riseTime) / absorbTime;
            if (t < 0f) return 0f;
            return t > 1f ? 1f : t;
        }

        // 흡수 구간에서만 투명해진다. fadeStart 이전에는 완전 불투명
        public static float Alpha(float absorbT, float fadeStart)
        {
            if (absorbT <= fadeStart) return 1f;
            if (fadeStart >= 1f) return 1f;
            float k = (absorbT - fadeStart) / (1f - fadeStart);
            if (k > 1f) k = 1f;
            return 1f - k;
        }

        public static float ScaleAt(float absorbT, float from, float to)
        {
            return from + (to - from) * EaseIn(absorbT);
        }

        // 채울 슬롯 번호. 이미 다 찼거나 용량이 없으면 -1
        public static int NextSlot(int filled, int capacity)
        {
            if (filled < 0 || capacity <= 0 || filled >= capacity) return -1;
            return filled;
        }

        // 슬롯 등장 팝: 0 → popPeak → 1
        public static float PopScale(float t, float popTime, float popPeak)
        {
            if (popTime <= 0f) return 1f;
            float k = t / popTime;
            if (k < 0f) k = 0f;
            if (k >= 1f) return 1f;
            if (k < 0.5f) return popPeak * EaseOut(k * 2f);
            return popPeak + (1f - popPeak) * EaseOut((k - 0.5f) * 2f);
        }
    }
}
