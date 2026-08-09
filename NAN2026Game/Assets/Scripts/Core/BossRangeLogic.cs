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

        /// 세로 제한이 있는 근접 판정. 기존 InHitBand 에 '발끝 높이' 조건을 더한다.
        /// selfFootY 는 공격자의 발끝(접지) y, targetFootY 는 대상의 발끝 y.
        /// 대상 발끝이 공격자 발끝 + attackHeight 를 넘어가면(= 뛰어넘은 상태) 맞지 않는다.
        /// 보스 호출부를 건드리지 않도록 **오버로드**로 추가한다.
        public static bool InHitBand(float bossX, float targetX, float reach, float facingSign, float deadZone,
                                     float selfFootY, float targetFootY, float attackHeight)
        {
            if (!InHitBand(bossX, targetX, reach, facingSign, deadZone)) return false;
            if (attackHeight <= 0f) return true;              // 0 이하면 세로 제한 없음(기존 동작)
            float rel = targetFootY - selfFootY;
            if (rel > attackHeight) return false;             // 머리 위로 뛰어넘음
            if (rel < -attackHeight) return false;            // 아래층에 있음
            return true;
        }

        /// 좌우 양쪽으로 퍼지는 공격(스매시 충격파)용 판정. 바라보는 방향과 무관.
        public static bool InHitBandBothSides(float bossX, float targetX, float reach)
        {
            float d = targetX - bossX;
            float ad = d < 0f ? -d : d;
            return ad <= reach;
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
