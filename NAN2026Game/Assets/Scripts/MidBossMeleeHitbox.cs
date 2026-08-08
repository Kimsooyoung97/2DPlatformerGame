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

            // 이 오브젝트들(Normal/Fire/Wheel/Bomb)은 전부 MidBoss의 자식이고
            // MidBoss 자신이 이미 Rigidbody2D를 갖고 있다. 여기에 또 붙이면 부모-자식에
            // Rigidbody2D가 중첩되는데, 이는 Unity 2D 물리가 공식적으로 지원하지 않는
            // 구성이라 예측 불가능한 동작(성능 저하·행 등)을 일으킬 수 있어 붙이지 않는다.
            // 트리거 판정은 상대(플레이어) 쪽에 Rigidbody2D가 있으면 정상 작동한다.
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (hasResolved) return;
            if (!other.CompareTag("Player")) return;

            float timer = 0f;
            float endtime = 0.5f;
            PlayerController2D pc = other.GetComponentInParent<PlayerController2D>();
            while (timer < endtime)
            {
                timer += Time.deltaTime;
                if (pc != null && pc.TryParry(bossObject))
                {
                    hasResolved = true;
                    Debug.Log("패링성공");// 패링당하면 판정 종료(피해 없음)
                    return;
                }
            }
            
            

            PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Debug.Log("패링실패");
                hasResolved = true;
            }
        }
    }
}
