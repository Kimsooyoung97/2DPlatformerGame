namespace NAN2026.Core
{
    // 전장의 안개: 거리 기반 밝힘 계수 + 가시선 차폐 판정. 순수 로직 (UnityEngine 무의존).
    public static class FogLogic
    {
        public static float RevealFactor(float distance, float radius, float softEdge)
        {
            if (distance <= radius) return 1f;
            if (softEdge <= 0f || distance >= radius + softEdge) return 0f;
            return 1f - (distance - radius) / softEdge;
        }

        public static bool ShouldRestamp(float dx, float dy, float moveThreshold)
        {
            return dx * dx + dy * dy >= moveThreshold * moveThreshold;
        }

        // 방향 (dx,dy) → 각도 버킷 인덱스 [0, buckets)
        public static int AngleBucket(float dx, float dy, int buckets)
        {
            double angle = System.Math.Atan2(dy, dx); // [-π, π]
            double norm = (angle + System.Math.PI) / (2.0 * System.Math.PI); // [0, 1]
            int idx = (int)(norm * buckets);
            if (idx >= buckets) idx = buckets - 1;
            if (idx < 0) idx = 0;
            return idx;
        }

        // 차단 거리 대비 가시 여부 (관용치만큼은 차단면 자체도 보이게)
        public static bool VisibleAt(float distance, float blockedDistance, float tolerance)
        {
            return distance <= blockedDistance + tolerance;
        }
    }
}
