namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 보스 근접 판정 '띠(band)' 기하. 실제 타격 판정과 디버그 표시가 같은 함수를 쓴다.
    /// 데몬 보스 판정 = 수평거리 <= reach && 바라보는 쪽 (수직 제한 없음).
    public static class BossRangeLogic
    {
        /// 판정 띠의 왼쪽 끝 X.
        public static float BandMinX(float bossX, float reach, float facingSign, float deadZone)
        {
            return facingSign < 0f ? bossX - reach : bossX - deadZone;
        }

        /// 판정 띠의 오른쪽 끝 X.
        public static float BandMaxX(float bossX, float reach, float facingSign, float deadZone)
        {
            return facingSign < 0f ? bossX + deadZone : bossX + reach;
        }

        /// 대상이 실제 타격 띠 안에 있는가. DemonBoss 의 타격 게이트와 동일 판정.
        public static bool InHitBand(float bossX, float targetX, float reach, float facingSign, float deadZone)
        {
            float d = targetX - bossX;
            float ad = d < 0f ? -d : d;
            if (ad > reach) return false;
            return BossFacingLogic.TargetInFront(bossX, targetX, facingSign, deadZone);
        }

        /// 애니메이션 진행률이 타격 시간창 안인가.
        public static bool WindowOpen(float frac, float winStart, float winEnd)
        {
            return frac >= winStart && frac <= winEnd;
        }

        /// 타격 시간창까지 남은 진행률(0 이하면 이미 열림/지남). 예고 표시용.
        public static float FracUntilWindow(float frac, float winStart)
        {
            return winStart - frac;
        }
    }
}
