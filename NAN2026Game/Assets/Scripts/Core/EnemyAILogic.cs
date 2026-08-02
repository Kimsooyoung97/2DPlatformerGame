namespace NAN2026.Core
{
    public enum EnemyAIState { Patrol, Chase, Attack }

    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    public static class EnemyAILogic
    {
        /// wasEngaged=true면 이미 추적/공격 중이었다는 뜻 — chaseStopDistance를
        /// 넘어서기 전까지는 attackRange 밖이라도 Patrol로 돌아가지 않고 Chase를 유지한다.
        public static EnemyAIState DetermineState(
            float distanceToPlayer,
            bool wasEngaged,
            float aggroRange,
            float attackRange,
            float chaseStopDistance)
        {
            if (wasEngaged)
            {
                if (distanceToPlayer > chaseStopDistance) return EnemyAIState.Patrol;
                if (distanceToPlayer <= attackRange) return EnemyAIState.Attack;
                return EnemyAIState.Chase;
            }

            if (distanceToPlayer <= aggroRange)
            {
                return distanceToPlayer <= attackRange ? EnemyAIState.Attack : EnemyAIState.Chase;
            }

            return EnemyAIState.Patrol;
        }

        /// 목표(플레이어)가 jumpYThreshold 이상 높은 층에 있고 현재 접지 상태일 때만 점프가 필요하다.
        public static bool NeedsJumpToFollow(float selfY, float targetY, float jumpYThreshold, bool isGrounded)
        {
            return isGrounded && (targetY - selfY) >= jumpYThreshold;
        }

        /// 좌우 경계에 닿으면 방향을 뒤집는 순찰 방향 결정.
        public static float PatrolDirection(float currentX, float leftBoundX, float rightBoundX, float previousDirection)
        {
            if (currentX <= leftBoundX) return 1f;
            if (currentX >= rightBoundX) return -1f;
            return previousDirection == 0f ? 1f : previousDirection;
        }

        /// 0~1로 클램프된 체력 비율.
        public static float HealthRatio(float current, float max)
        {
            if (max <= 0f) return 0f;
            float r = current / max;
            if (r < 0f) return 0f;
            if (r > 1f) return 1f;
            return r;
        }
    }
}
