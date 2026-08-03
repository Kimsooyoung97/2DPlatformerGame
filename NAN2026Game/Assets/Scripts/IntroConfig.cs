using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "IntroConfig", menuName = "NAN2026/IntroConfig")]
    public class IntroConfig : ScriptableObject
    {
        [Header("페이즈 길이(초)")]
        public float blackSeconds = 0.5f;
        public float igniteSeconds = 0.9f;
        public float expandSeconds = 1.2f;

        [Header("촛불 조명")]
        public float candleIntensity = 1.1f;
        public float candleOuterRadius = 1.7f;
        public float candleInnerRadius = 0.25f;
        public Color candleColor = new Color(1f, 0.72f, 0.35f, 1f);

        [Header("전역 조명")]
        public float globalMaxIntensity = 1f;

        [Header("BGM")]
        public float bgmVolume = 0.55f;
        public float bgmFadeSeconds = 1.2f;
    }
}
