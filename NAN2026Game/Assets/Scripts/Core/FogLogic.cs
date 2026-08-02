namespace NAN2026.Core
{
    // 전장의 안개: 거리 기반 밝힘 계수 (0=안개 유지, 1=완전 밝힘). 순수 로직.
    public static class FogLogic
    {
        public static float RevealFactor(float distance, float radius, float softEdge)
        {
            if (distance <= radius) return 1f;
            if (softEdge <= 0f || distance >= radius + softEdge) return 0f;
            return 1f - (distance - radius) / softEdge;
        }

        // 갱신 필요 판정: 마지막 스탬프 위치에서 임계 이상 이동했는가
        public static bool ShouldRestamp(float dx, float dy, float moveThreshold)
        {
            return dx * dx + dy * dy >= moveThreshold * moveThreshold;
        }
    }
}
