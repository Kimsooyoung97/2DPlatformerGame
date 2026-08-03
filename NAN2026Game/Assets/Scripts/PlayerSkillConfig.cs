using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "PlayerSkillConfig", menuName = "NAN2026/PlayerSkillConfig")]
    public class PlayerSkillConfig : ScriptableObject
    {
        public float skillFps;        // 스킬대기 재생 fps
        public int triggerFrame;      // 이펙트 시작 프레임 (1-기준)
        public int sideCount;         // 한쪽 이펙트 개수
        public float startOffset;     // 첫 이펙트 X 거리
        public float spacing;         // 이펙트 간격
        public float stagger;         // 쌍 사이 시간차
        public float effectFps;       // 이펙트 재생 fps
        public float effectScale;
        public int effectSortingOrder;
        public float cooldown;
        public float groundSnapDepth;
        public int startFrame;         // 모션 시작 컷 (0-기준, 예비 동작 생략용)  // 이펙트 지면 탐색 깊이 (없으면 생략)
    }
}
