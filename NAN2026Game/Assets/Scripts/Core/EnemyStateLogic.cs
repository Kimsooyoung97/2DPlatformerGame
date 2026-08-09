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
        public const int Windup = 5;
        public const int Groggy = 6;      // 패링 성공으로 무방비. 플레이어의 보상 구간   // 공격 예열(경고). 이 시간이 곧 플레이어의 반응 시간이다

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
        /// 공격 전용 fps. 0 이하면 공용 fps 를 쓴다.
        /// 걷기·대기 속도를 건드리지 않고 휘두름만 늦추기 위해 분리한다.
        /// 패링 성공으로 들어간 그로기가 끝났는가.
        public static bool GroggyFinished(float elapsed, float dur)
        {
            return elapsed >= dur;
        }

        /// 반사된 화살이 살아 있어야 하는 시간.
        /// 남은 수명이 짧으면 쏜 사람에게 닿기 전에 사라지므로 최소 수명을 보장한다.
        public static float ReflectLife(float remainingLife, float minLife)
        {
            return remainingLife < minLife ? minLife : remainingLife;
        }

        /// 반사된 화살의 속도. mul 이 0 이하면 원래 속도를 그대로 쓴다.
        public static float ReflectSpeed(float speed, float mul)
        {
            return mul > 0f ? speed * mul : speed;
        }

        /// 잡몹 한 번의 휘두름에서 이번 프레임에 무엇을 할지 결정한다.
        /// 0 = 아무것도 안 함, 1 = 패링 접수(물어본다), 2 = 데미지 확정.
        /// 판정을 창의 '첫 프레임'이 아니라 '끝'에서 내리는 것이 핵심 —
        /// 창이 열려 있는 동안 매 프레임 패링을 접수하므로 늦게 눌러도 인정된다.
        public static int SwingResolve(float frac, float winStart, float winEnd, bool alreadyResolved)
        {
            if (alreadyResolved) return 0;
            if (frac >= winEnd) return 2;
            if (frac >= winStart) return 1;
            return 0;
        }

        public static float AttackFps(float attackFps, float baseFps)
        {
            return attackFps > 0f ? attackFps : baseFps;
        }

        /// 프레임 수와 fps 로부터 모션이 끝나는 시간.
        /// attackDur 가 이 값과 어긋나면 모션이 잘리거나 마지막 프레임에서 늘어진다.
        public static float DurationForFrames(int frameCount, float fps)
        {
            if (frameCount <= 0 || fps <= 0f) return 0f;
            return frameCount / fps;
        }

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
