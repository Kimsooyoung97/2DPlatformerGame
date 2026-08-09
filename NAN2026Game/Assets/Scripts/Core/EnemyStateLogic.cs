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
        public const int Windup = 5;   // 공격 예열(경고). 이 시간이 곧 플레이어의 반응 시간이다

        /// 평상시 상태 결정. distX 는 수평 거리(절대값).
        /// 사거리 안이고 쿨다운이 끝났으면 공격, 인지 범위 안이면 접근, 아니면 대기.
        public static int Decide(float distX, float aggroRange, float attackRange, bool attackReady)
        {
            if (distX <= attackRange && attackReady) return Attack;
            if (distX <= aggroRange) return Walk;
            return Idle;
        }

        /// 정지-대기형 판단. 사거리 안이면 쿨다운이 끝났을 때만 공격하고,
        /// 쿨다운 중에는 **더 다가가지 않고 대기(Idle)** 한다.
        /// 기존 Decide 는 쿨다운 중 Walk 를 반환해 적이 플레이어를 관통해 지나갔다(다수 배치 시 한 점에 겹침).
        public static int DecideWithHold(float distX, float aggroRange, float attackRange, bool attackReady)
        {
            if (distX <= attackRange) return attackReady ? Attack : Idle;
            if (distX <= aggroRange) return Walk;
            return Idle;
        }

        /// 이번 프레임에 허용되는 접근 거리. stopDistance 안쪽으로는 파고들지 않는다.
        public static float MoveStep(float distX, float stopDistance, float speed, float dt)
        {
            if (speed <= 0f || dt <= 0f) return 0f;
            float room = distX - stopDistance;
            if (room <= 0f) return 0f;
            float step = speed * dt;
            return step > room ? room : step;
        }

        /// 진행 방향 앞쪽 separation 안에 동료가 있으면 멈춘다(겹침 방지).
        public static bool BlockedByNeighbor(float selfX, float neighborX, float moveSign, float separation)
        {
            if (separation <= 0f) return false;
            float d = (neighborX - selfX) * moveSign;   // 양수면 진행 방향 앞
            return d > 0f && d < separation;
        }

        /// 쿨다운에 편차를 준다. rand01 은 0~1. 동일 쿨다운으로 인한 영구 동기화를 깬다.
        public static float JitteredCooldown(float baseCooldown, float jitter, float rand01)
        {
            if (jitter <= 0f) return baseCooldown;
            float v = baseCooldown + (rand01 - 0.5f) * jitter;
            return v < 0f ? 0f : v;
        }

        /// 최초 공격 준비까지의 랜덤 지연. 동시 진입 시 첫 발이 겹치는 것을 막는다.
        public static float InitialDelay(float stagger, float rand01)
        {
            if (stagger <= 0f) return 0f;
            return rand01 * stagger;
        }

        /// 예열 종료 여부.
        public static bool WindupFinished(float elapsed, float windupDur)
        {
            return windupDur <= 0f || elapsed >= windupDur;
        }

        /// 경고 점멸 세기 0~1. Mathf.PingPong 과 동일한 삼각파(UnityEngine 비의존).
        public static float FlashPulse01(float elapsed, float speed)
        {
            if (speed <= 0f) return 0f;
            float x = elapsed * speed;
            float m = x - 2f * (float)System.Math.Floor(x / 2f);   // 0~2 로 접기
            return m > 1f ? 2f - m : m;
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
