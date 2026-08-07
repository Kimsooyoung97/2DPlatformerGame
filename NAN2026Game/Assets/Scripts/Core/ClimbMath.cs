namespace NAN2026.Core
{
    // 등반 속도 계산 — 순수 함수
    public static class ClimbMath
    {
        public static float ClimbVelocity(bool up, bool down, float speed)
        {
            if (up == down) return 0f;
            return up ? speed : -speed;
        }
    }
}
