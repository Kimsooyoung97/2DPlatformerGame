namespace NAN2026.Core
{
    // 스파이크볼 상태·경고 점멸·조준 — 순수 로직
    public static class SpikeBallLogic
    {
        // 0=대기, 1=경고(점멸), 2=발사
        public static int Phase(float dist, float visionRadius, float warnMultiplier, float launchMultiplier)
        {
            if (dist <= visionRadius * launchMultiplier) return 2;
            if (dist <= visionRadius * warnMultiplier) return 1;
            return 0;
        }

        // 점멸 알파 (0.35~1.0 왕복)
        public static float BlinkAlpha(float time, float hz)
        {
            float s = (float)System.Math.Sin(time * hz * 6.28318f);
            return 0.675f + 0.325f * s;
        }

        // 정규화 발사 방향
        public static void LaunchDir(float fx, float fy, float tx, float ty, out float dx, out float dy)
        {
            dx = tx - fx; dy = ty - fy;
            float m = (float)System.Math.Sqrt(dx * dx + dy * dy);
            if (m < 0.0001f) { dx = 0f; dy = -1f; return; }
            dx /= m; dy /= m;
        }
    }
}
