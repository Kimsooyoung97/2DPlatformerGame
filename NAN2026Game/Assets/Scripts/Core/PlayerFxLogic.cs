namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 플레이어 피격·사망 연출의 발동 조건과 길이 계산.
    public static class PlayerFxLogic
    {
        /// 체력이 줄었고 아직 살아 있으면 피격 연출을 재생한다.
        /// 회복·최대체력 증가(증가 방향)나 사망(0 이하)에서는 재생하지 않는다.
        public static bool ShouldPlayHurt(int prevHealth, int currentHealth)
        {
            return currentHealth < prevHealth && currentHealth > 0;
        }

        /// 사망 연출인가.
        public static bool ShouldPlayDeath(int currentHealth)
        {
            return currentHealth <= 0;
        }

        /// 시트 재생 길이(초) = 프레임수/fps + 마지막 프레임 유지시간.
        public static float Duration(int frameCount, float fps, float hold)
        {
            if (frameCount <= 0 || fps <= 0f) return hold < 0f ? 0f : hold;
            float d = frameCount / fps + (hold < 0f ? 0f : hold);
            return d;
        }

        /// 부활까지 필요한 지연 = 사망 연출 길이. PlayerHealth.respawnDelay 가 이보다 짧으면
        /// 연출이 잘리므로, 설정값과 실제 길이를 비교해 더 큰 쪽을 쓴다.
        public static float RespawnDelay(float configuredDelay, float deathDuration)
        {
            return configuredDelay > deathDuration ? configuredDelay : deathDuration;
        }
    }
}
