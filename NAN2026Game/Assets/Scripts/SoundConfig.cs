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
        [Header("스킬 사운드 (1/2/3키)")]
        public AudioClip skill1Clip;
        public AudioClip skill2Clip;
        public AudioClip skill3Clip;
        [Range(0f, 1f)] public float skillVolume = 1f;
        public float skillPitch = 1f;
        [Header("피격 (2종 랜덤)")]
        public AudioClip[] hitClips = new AudioClip[2];
        [Range(0f, 1f)] public float hitVolume = 1f;
        [Header("사망 (2종 랜덤)")]
        public AudioClip[] deathClips = new AudioClip[2];
        [Range(0f, 1f)] public float deathVolume = 1f;
        [Header("씬 BGM")]
        public float bgmVolume = 0.55f;
        public float bgmFadeSeconds = 1.2f;

        /// <summary>배열에서 null 아닌 클립 중 하나를 균등 랜덤 선택. 비어있으면 null.
        /// 슬롯 일부가 아직 안 채워져도(예: 1개만 등록) 안전하게 동작한다.</summary>
        public AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                var c = clips[(start + i) % clips.Length];
                if (c != null) return c;
            }
            return null;
        }
    }
}