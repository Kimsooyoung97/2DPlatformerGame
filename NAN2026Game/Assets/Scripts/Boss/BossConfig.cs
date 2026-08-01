using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "BossConfig", menuName = "Game/BossConfig")]
    public class BossConfig : ScriptableObject
    {
        [Header("등장 시퀀스")]
        public int idle1Loops = 2;

        [Header("구체 공격")]
        public float orbInterval = 1.6f;
        public float orbSpeed = 6f;
        public float orbLifetime = 4f;
        public float orbSpawnHeight = 2.6f;
    }
}