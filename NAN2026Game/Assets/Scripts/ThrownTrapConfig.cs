using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "ThrownTrapConfig", menuName = "NAN2026/ThrownTrapConfig")]
    public class ThrownTrapConfig : ScriptableObject
    {
        [Header("공통")]
        public float aggroX = 11f;          // 이 가로거리 안에 플레이어가 오면 가동
        public float telegraphTime = 0.45f; // 전조(철컥)~발사
        public int damage = 1;
        public float parryReach = 1.6f;     // 패링 인정 거리(투사체-플레이어)
        public float lifeTime = 6f;
        [Header("화살")]
        public float arrowSpeed = 9f;
        public float arrowCooldown = 2.4f;
        public int arrowMp = 8;
        [Header("수리검 (연발)")]
        public float shurikenSpeed = 6.5f;
        public int shurikenBurst = 3;
        public float shurikenInterval = 0.28f;
        public float shurikenCooldown = 3.6f;
        public float shurikenSpin = 720f;
        public int shurikenMp = 6;
        [Header("도끼 (포물선)")]
        public float axeSpeed = 5.5f;
        public float axeUpVel = 6.5f;
        public float axeGravity = 1.6f;
        public float axeCooldown = 4.5f;
        public float axeSpin = 420f;
        public int axeMp = 15;
        public int axeMpPerfect = 30;
        public float axeReflectSpeed = 9f;  // 패링 반사 속도(발사기 파괴)
        [Header("사운드")]
        public AudioClip sndTelegraph;
        public AudioClip sndArrow;
        public AudioClip sndSpin;
        public AudioClip sndAxeImpact;
        public AudioClip sndLauncherBreak;
    }
}
