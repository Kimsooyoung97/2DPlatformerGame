namespace NAN2026.Core
{
    /// <summary>
    /// 피격 깜빡임의 순수 판정 로직. MonoBehaviour와 Unity 런타임에 의존하지 않으므로
    /// EditMode 테스트로 단독 검증할 수 있다.
    /// 수치는 갖지 않는다. 호출자가 FeelConfig에서 받아 넘긴다.
    /// </summary>
    public static class HitFlashBlinker
    {
        /// <summary>
        /// 깜빡임 시작 후 elapsed초 시점에 대상이 보여야 하는지 반환한다.
        /// interval초마다 보임/숨김이 번갈아 뒤집힌다.
        /// </summary>
        public static bool IsVisible(float elapsed, float interval)
        {
            if (interval <= 0f)
                return true;

            if (elapsed < 0f)
                return true;

            int step = (int)System.Math.Floor(elapsed / interval);
            return step % 2 == 0;
        }

        /// <summary>
        /// 깜빡임이 끝났는지 반환한다. duration이 0 이하면 즉시 종료로 본다.
        /// </summary>
        public static bool IsFinished(float elapsed, float duration)
        {
            if (duration <= 0f)
                return true;

            return elapsed >= duration;
        }
    }
}
