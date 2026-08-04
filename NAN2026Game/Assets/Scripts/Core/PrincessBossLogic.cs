namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    public static class PrincessBossLogic
    {
        /// QTE 비트: 목표 시각(beatTargetTime)과 실제 입력 시각(pressTime)의 차이가
        /// 허용 오차(hitWindow) 이내면 성공.
        public static bool IsBeatHit(float beatTargetTime, float pressTime, float hitWindow)
        {
            float diff = beatTargetTime - pressTime;
            if (diff < 0f) diff = -diff;
            return diff <= hitWindow;
        }

        /// 모든 비트를 다 맞혀야(hitCount == beatCount) QTE 성공.
        public static bool QteSucceeded(int hitCount, int beatCount)
        {
            return hitCount >= beatCount;
        }
    }
}
