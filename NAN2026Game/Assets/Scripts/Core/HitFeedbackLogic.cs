namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 피격 피드백(넉백·히트스톱·플래시)의 계산부. 수치는 호출자가 FeelConfig 에서 넘긴다.
    public static class HitFeedbackLogic
    {
        /// 넉백 방향. 가해자 위치를 알면 그 반대로, 모르면 바라보는 반대쪽으로 민다.
        /// hasSource=false 면 facingSign 의 반대를 쓴다.
        public static float KnockbackSign(bool hasSource, float selfX, float sourceX, float facingSign)
        {
            if (hasSource)
            {
                if (sourceX < selfX) return 1f;   // 왼쪽에서 맞았으면 오른쪽으로
                if (sourceX > selfX) return -1f;
                return facingSign >= 0f ? -1f : 1f; // 완전히 겹쳤으면 바라보는 반대
            }
            return facingSign >= 0f ? -1f : 1f;
        }

        /// 넉백 진행률(0~1)에 따른 이동량. 앞이 빠르고 뒤가 느린 감쇠(1-(1-t)^2 의 미분 형태 근사).
        /// 프레임당 이동량 = 총거리 * (감쇠계수) * (dt/duration)
        public static float KnockbackStep(float distance, float elapsed, float duration, float dt)
        {
            if (duration <= 0f || distance == 0f) return 0f;
            if (elapsed >= duration) return 0f;
            float t = elapsed / duration;
            float w = 2f * (1f - t);           // 선형 감쇠 가중치. 적분하면 총 1
            return distance * w * (dt / duration);
        }

        /// 히트스톱이 끝났는가. unscaled 시간으로 판정해야 timeScale 0 에서도 흐른다.
        public static bool HitStopFinished(float unscaledNow, float restoreAt)
        {
            return restoreAt <= 0f || unscaledNow >= restoreAt;
        }

        /// 히트스톱 길이를 무적시간보다 길게 두면 조작 불능이 길어진다. 상한을 건다.
        public static float ClampHitStop(float requested, float invincibleDuration)
        {
            if (requested < 0f) return 0f;
            float cap = invincibleDuration * 0.25f;
            return requested > cap ? cap : requested;
        }
    }
}
