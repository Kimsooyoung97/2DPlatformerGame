using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "PlatformConfig", menuName = "Game/PlatformConfig")]
    public class PlatformConfig : ScriptableObject
    {
        public float disappearDelay = 0.8f;
        public float respawnDelay = 2.5f;
        public float blinkHz = 6f;
    }
}