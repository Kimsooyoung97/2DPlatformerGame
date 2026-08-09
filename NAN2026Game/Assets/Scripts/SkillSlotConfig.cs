using UnityEngine;

namespace NAN2026
{
    // 6·7번 스킬 수치 단일 소유 (MonoBehaviour 숫자 리터럴 금지 규약)
    [CreateAssetMenu(fileName = "SkillSlotConfig", menuName = "NAN2026/SkillSlotConfig")]
    public class SkillSlotConfig : ScriptableObject
    {
        [Header("공통")]
        public int mpCost = 2;
        public float cooldown = 0.8f;
        public float spawnForward = 0.8f;   // 몸 앞 발사 거리
        public float spawnHeight = 0.5f;
        [Header("투사체")]
        public float speed = 12f;
        public float life = 2.5f;
        public int damage = 2;
        public float fps = 14f;
        public float hitboxSize = 0.6f;
        public bool piercing = false;
        public float scale = 1f;
    }
}
