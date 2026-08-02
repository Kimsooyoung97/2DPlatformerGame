using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "AttackEffectConfig", menuName = "Game/AttackEffectConfig")]
    public class AttackEffectConfig : ScriptableObject
    {
        [Header("발사")]
        public float basicSpeed = 7f;
        public float poweredSpeed = 9f;
        public float lifetime = 0.8f;
        public Vector2 spawnOffset = new Vector2(0.45f, 0.95f);

        [Header("크기 (캐릭터 대비)")]
        public float basicScale = 5.9f;
        public float poweredScale = 6.6f;

        [Header("애니메이션")]
        public float frameRate = 16f;

        [Header("데미지")]
        public int basicDamage = 1;
        public int poweredDamage = 3;
        public Vector2 hitboxSize = new Vector2(0.9f, 0.9f);
    }
}