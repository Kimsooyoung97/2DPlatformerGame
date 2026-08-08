namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 확산 발사(부채꼴) 각도 계산. 유도가 아니라 고정 부채꼴이어야 회피가 성립한다.
    public static class SpreadShotLogic
    {
        /// index 번째 탄의 각도(도). 바라보는 방향을 0도로 보는 로컬 각도.
        /// baseDeg = 부채꼴 중심 각도(음수면 아래쪽), spreadDeg = 전체 벌어짐.
        /// count==1 이면 baseDeg 그대로.
        public static float AngleDeg(int index, int count, float baseDeg, float spreadDeg)
        {
            if (count <= 1) return baseDeg;
            if (index < 0) index = 0;
            if (index > count - 1) index = count - 1;
            float step = spreadDeg / (count - 1);
            return baseDeg - spreadDeg * 0.5f + step * index;
        }

        /// 부채꼴의 양 끝 각도.
        public static float MinAngleDeg(float baseDeg, float spreadDeg) { return baseDeg - spreadDeg * 0.5f; }
        public static float MaxAngleDeg(float baseDeg, float spreadDeg) { return baseDeg + spreadDeg * 0.5f; }

        /// 발사 지연(초). 동시 발사가 아니라 살짝 흩뿌리고 싶을 때.
        public static float FireDelay(int index, float perShotDelay)
        {
            return index < 0 ? 0f : index * perShotDelay;
        }
    }
}
