using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "SpikeBallConfig", menuName = "NAN2026/SpikeBallConfig")]
    public class SpikeBallConfig : ScriptableObject
    {
        [Header("감지 (시야광 반경 배수)")]
        public float warnMultiplier = 2f;
        public float launchMultiplier = 1.1f;
        public float visionRadiusFallback = 4.5f;

        [Header("경고 점멸")]
        public float blinkHz = 5f;

        [Header("돌진")]
        public float launchSpeed = 13f;
        public float spinDegPerSec = 540f;

        [Header("판정")]
        public int damage = 1;
        public float deflectSpeed = 9f;
        public float respawnDelay = 3f;

        [Header("격돌 이펙트(할로우나이트식)")]
        public float clashDuration = 0.16f;
        public int clashLines = 8;
        public float clashRadius = 1.3f;
        public float clashHitstop = 0.08f;
        public AudioClip clashSound;
        [Range(0f,1f)] public float clashVolume = 0.9f;
        public float clashSoundStartMs = 0f;   // 재생 시작(ms)
        public float clashSoundEndMs = 864f;  // 재생 끝(ms, 이 구간만 사용)

        [Header("팝업")]
        public float popupRise = 1.2f;
        public float popupLife = 0.9f;
        public int popupFontSize = 64;
        public float popupCharSize = 0.06f;
    }
}
