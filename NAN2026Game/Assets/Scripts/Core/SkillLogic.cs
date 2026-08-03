namespace NAN2026.Core
{
    // 스킬 이펙트 배치·타이밍 순수 로직
    public static class SkillLogic
    {
        // n번째(0부터) 이펙트의 X 오프셋 (좌우 대칭 쌍의 한쪽)
        public static float OffsetX(int index, float startOffset, float spacing)
        {
            return startOffset + index * spacing;
        }

        // k프레임 시작 시각 (1-기준 프레임 번호)
        public static float FrameTime(int frame, float fps)
        {
            if (fps <= 0f) return 0f;
            int f = frame < 1 ? 1 : frame;
            return (f - 1) / fps;
        }
    }
}
