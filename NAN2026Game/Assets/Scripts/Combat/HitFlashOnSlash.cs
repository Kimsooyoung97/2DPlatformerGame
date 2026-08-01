using UnityEngine;
using NAN2026.Core;
using NAN2026.Showroom;

namespace NAN2026.Combat
{
    /// <summary>
    /// 검기에 맞으면 스프라이트가 깜빡이는 피격 반응 더미.
    /// S1 조작감 판정에서 타격 피드백 수치를 눈으로 맞추기 위한 대상이다.
    /// 파괴되지 않는다. (SPEC.md — 범위 밖: 파괴 가능한 환경 오브젝트)
    ///
    /// 자기 트리거로 SlashProjectile을 직접 감지하므로
    /// 기존 SlashProjectile.cs를 수정하지 않는다.
    ///
    /// 모든 수치는 FeelConfig가 소유한다. 이 클래스에 숫자 리터럴은 없다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class HitFlashOnSlash : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("깜빡임 수치의 단일 기준")]
        [SerializeField] private FeelConfig feelConfig;

        [Tooltip("깜빡일 대상. 비우면 자식에서 찾는다")]
        [SerializeField] private SpriteRenderer targetRenderer;

        private bool isFlashing;
        private float elapsed;
        private int hitCount;

        /// <summary>지금까지 맞은 횟수. 검증용.</summary>
        public int HitCount => hitCount;

        /// <summary>깜빡이는 중인지 여부. 검증용.</summary>
        public bool IsFlashing => isFlashing;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SpriteRenderer>();

            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
                return;

            if (other.GetComponentInParent<SlashProjectile>() == null)
                return;

            hitCount++;
            StartFlash();
        }

        private void StartFlash()
        {
            isFlashing = true;
            elapsed = 0f;
        }

        private void Update()
        {
            if (!isFlashing)
                return;

            elapsed += Time.deltaTime;

            if (feelConfig == null)
            {
                StopFlash();
                return;
            }

            if (HitFlashBlinker.IsFinished(elapsed, feelConfig.hitFlashDuration))
            {
                StopFlash();
                return;
            }

            if (targetRenderer != null)
                targetRenderer.enabled = HitFlashBlinker.IsVisible(elapsed, feelConfig.hitFlashInterval);
        }

        private void StopFlash()
        {
            isFlashing = false;

            if (targetRenderer != null)
                targetRenderer.enabled = true;
        }
    }
}
