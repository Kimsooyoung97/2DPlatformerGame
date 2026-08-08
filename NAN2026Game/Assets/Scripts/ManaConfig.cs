using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "ManaConfig", menuName = "NAN2026/ManaConfig")]
    public class ManaConfig : ScriptableObject
    {
        public int maxMp = 10;        // 팀 명세: 총량 10
        public int parryGain = 1;     // 팀 명세: 패링 성공 +1
        public int startMp = 0;
        [Header("HUD")]
        public Sprite heartFull;      // 파란하트 _0
        public Sprite heartEmpty;     // 파란하트 _2
        public float heartSize = 28f;
        public float heartSpacing = 30f;
        public Vector2 hudOffset = new Vector2(20f, -64f); // 좌상단 기준(HP 아래)
    }
}
