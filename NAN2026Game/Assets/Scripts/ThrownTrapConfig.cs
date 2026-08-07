using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "ThrownTrapConfig", menuName = "NAN2026/ThrownTrapConfig")]
    public class ThrownTrapConfig : ScriptableObject
    {
        [Header("공통")]
        public float aggroX = 11f;
        public float telegraphTime = 0.45f;
        public int damage = 1;
        public float parryReach = 1.6f;
        public float lifeTime = 6f;
        public float ballSpin = 360f;      // 스파이크볼 회전
        public float reflectSpeed = 9f;    // 패링 반사(발사기 파괴)
        [Header("발광")]
        public float glowIntensity = 2.2f;
        public float glowRadius = 2.4f;
        public Color glowColor = new Color(1f, 0.92f, 0.6f, 1f);
        [Header("속도·쿨다운")]
        public float arrowSpeed = 9f;
        public float arrowCooldown = 2.4f;
        public float shurikenSpeed = 6.5f;
        public float shurikenCooldown = 3.6f;
        public float axeSpeed = 5.5f;
        public float axeCooldown = 4.5f;
        [Header("MP 보상")]
        public int arrowMp = 8;
        public int shurikenMp = 6;
        public int axeMp = 15;
        [Header("사운드 (발사음만)")]
        public AudioClip sndFire;
        public AudioClip sndLauncherBreak;
    }
}
