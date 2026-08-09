using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "NAN2026/SoundConfig")]
    public class SoundConfig : ScriptableObject
    {
        [Header("발소리")]
        public AudioClip[] walkClips;
        public float stepInterval = 0.32f;
        public float walkVelThreshold = 0.5f;
        public float stepVolume = 0.7f;

        [Header("점프·공격")]
        public AudioClip jumpClip;
        public AudioClip attackClip;
        public float jumpVolume = 0.8f;
        public float attackVolume = 0.8f;
        public float attackPitch = 0.85f;

        [Header("대시")]
        public AudioClip dashClip;
        public float dashVolume = 0.7f;
        public float dashPitch = 1f;

        [Header("씬 BGM")]
        public float bgmVolume = 0.55f;
        public float bgmFadeSeconds = 1.2f;
    }
}
