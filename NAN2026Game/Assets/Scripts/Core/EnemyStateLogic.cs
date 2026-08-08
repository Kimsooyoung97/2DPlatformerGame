namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 근접·원거리 잡몹 공용 상태 판단과 시트 애니메이션 인덱스 계산.
    public static class EnemyStateLogic
    {
        public const int Idle = 0;
        public const int Walk = 1;
        public const int Attack = 2;
        public const int Hurt = 3;
        public const int Death = 4;

        /// 평상시 상태 결정. distX 는 수평 거리(절대값).
        /// 사거리 안이고 쿨다운이 끝났으면 공격, 인지 범위 안이면 접근, 아니면 대기.
        public static int Decide(float distX, float aggroRange, float attackRange, bool attackReady)
        {
            if (distX <= attackRange && attackReady) return Attack;
            if (distX <= aggroRange) return Walk;
            return Idle;
        }

        /// 누적 피격 수가 사망 기준에 도달했는가.
        public static bool IsDead(int hitsTaken, int hitsToDie)
        {
            return hitsTaken >= hitsToDie;
        }

        /// 경과 시간 → 프레임 인덱스. loop=false 면 마지막 프레임에서 정지.
        public static int AnimIndex(float elapsed, float fps, int frameCount, bool loop)
        {
            if (frameCount <= 0) return 0;
            if (fps <= 0f) return 0;
            int i = (int)(elapsed * fps);
            if (i < 0) i = 0;
            if (loop) return i % frameCount;
            return i >= frameCount ? frameCount - 1 : i;
        }

        /// 비루프 애니메이션이 끝났는가.
        public static bool AnimFinished(float elapsed, float fps, int frameCount)
        {
            if (frameCount <= 0 || fps <= 0f) return true;
            return elapsed * fps >= frameCount;
        }

        /// 발사 타이밍이 되었는가. frac 은 공격 애니 진행률 0~1.
        public static bool ShouldFire(float frac, float fireFrac, bool alreadyFired)
        {
            return !alreadyFired && frac >= fireFrac;
        }

        /// 대상 쪽 월드 방향 (+1 오른쪽 / -1 왼쪽). 같으면 +1.
        public static float FaceSign(float selfX, float targetX)
        {
            return targetX < selfX ? -1f : 1f;
        }
    }
}
