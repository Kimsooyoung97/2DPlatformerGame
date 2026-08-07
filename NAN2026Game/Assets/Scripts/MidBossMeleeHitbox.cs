using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// MidBoss 근접 공격 시 보스 앞에 잠깐 생성되는 판정용 콜라이더.
    /// 거리 계산이 아니라 실제 트리거 겹침으로 판정한다 — 이 콜라이더 안에
    /// 플레이어가 들어와 있으면(겹쳐 있으면) 패링 체크 후 데미지를 준다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MidBossMeleeHitbox : MonoBehaviour
    {
        private int damage;
        private GameObject bossObject;
        private bool hasResolved;

        /// <summary>공격 시작 시 1회 호출. bossObject는 패링 방향 판정(IsAttackerInFront)에 쓰인다.</summary>
        public void Init(int damage, GameObject bossObject)
        {
            this.damage = damage;
            this.bossObject = bossObject;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (hasResolved) return;
            if (!other.CompareTag("Player")) return;

            PlayerController2D pc = other.GetComponentInParent<PlayerController2D>();
            if (pc != null && pc.TryParry(bossObject))
            {
                hasResolved = true; // 패링당하면 판정 종료(피해 없음)
                return;
            }

            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                hasResolved = true;
            }
        }
    }
}
