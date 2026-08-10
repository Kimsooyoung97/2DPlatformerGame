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

        [Header("소멸 조건 (씬마다 다름)")]
        [Tooltip("이 높이 아래로 내려가면 소멸. Scene2 기본 2.6")]
        public float killPlaneY = 2.6f;
        [Tooltip("발사 지점에서 이 거리를 넘으면 소멸")]
        public float maxTravel = 40f;
        [Tooltip("플레이어가 스파이크보다 아래에 있을 때만 작동. Scene2 는 false")]
        public bool onlyBelow = false;
        [Tooltip("조준 높이: 플레이어 발밑에서 몇 유닛 위를 노리는가")]
        public float aimHeight = 0.4f;
        [Tooltip("이동 중인 표적을 예측 조준한다(보트 탑승 구간용). Scene2 는 false")]
        public bool leadTarget = false;

        [Header("격돌 이펙트(할로우나이트식)")]
        public float clashDuration = 0.16f;
        public int clashLines = 8;
        public float clashRadius = 1.3f;
        public float clashHitstop = 0.08f;
        public bool clashRecoilEnabled = true;   // 해제 반동 켜기/끄기
        public float clashRecoilAmp = 0.06f;     // 반동 진폭(유닛)
        public float clashRecoilTime = 0.1f;     // 반동 시간(초)
        public Sprite clashSprite; // 격돌 이펙트 중앙 스프라이트 — 비워두면 기본 흰 점 폴백
        [Tooltip("중앙 스프라이트 전체 크기 배율(1=기본). 작게 하려면 1보다 작은 값(예: 0.5)")]
        public float clashFlashScale = 1f;
        public AudioClip clashSound;
        [Range(0f,1f)] public float clashVolume = 0.9f;
        public float clashSoundStartMs = 0f;   // 재생 시작(ms)
        public float clashSoundEndMs = 864f;  // 재생 끝(ms, 이 구간만 사용)

        [Header("팝업")]
        public float popupRise = 1.2f;
        public float popupLife = 0.9f;
        public int popupFontSize = 64;
        public float popupCharSize = 0.06f;
            public float homingTurn = 3.5f;   // 비행 중 유도 강도(0이면 직진)
        public float homingSeconds = 1.6f; // 유도 지속 시간(이후 직진)
}
}