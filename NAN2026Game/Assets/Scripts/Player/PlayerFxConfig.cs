using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "PlayerFxConfig", menuName = "NAN2026/PlayerFxConfig")]
    public class PlayerFxConfig : ScriptableObject
    {
        [Header("피격 연출")]
        public float hurtFps = 12f;
        public float hurtHold = 0.05f;

        [Header("사망 연출")]
        public float deathFps = 7f;
        public float deathHold = 0.45f;   // 마지막 프레임 유지 후 부활

        [Header("연출 중 입력")]
        public bool lockInputOnHurt = false;
        public bool lockInputOnDeath = true;

        [Header("디버그 미리보기 (제출 전 OFF)")]
        public bool enableDebugKeys = true;   // 4=hurt, 5=death (연출만, 실제 피해 없음)
    }
}
