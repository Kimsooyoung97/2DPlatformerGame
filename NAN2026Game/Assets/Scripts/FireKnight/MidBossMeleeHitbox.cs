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
        private System.Action onParried; // 패링 성공 시 발사 주체(보스)에게 알림 — 그로기 카운터 등에 사용
        private System.Func<bool> parryBufferedCheck; // Mino와 동일한 선입력 버퍼 판정(보스 쪽 기록) — 우선 확인

        /// <summary>공격 시작 시 1회 호출. bossObject는 패링 방향 판정(IsAttackerInFront)에 쓰인다.
        /// onParried는 패링 성공 시 1회 호출되는 콜백(선택) — 그로기 카운트 등에 사용.</summary>
        public void Init(int damage, GameObject bossObject, System.Action onParried = null, System.Func<bool> parryBufferedCheck = null)
        {
            this.damage = damage;
            this.bossObject = bossObject;
            this.onParried = onParried;
            this.parryBufferedCheck = parryBufferedCheck;

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

            // FAIL: 예전엔 여기서 0.5초짜리 동기 while 루프로 TryParry를 반복 폴링했다 —
            // Time.deltaTime이 그 프레임 내내 고정값이라 사실상 그 프레임이 멈추는
            // 프리즈 버그였다. 패링은 다른 보스(Demon/Mino)처럼 1회 체크로 충분하다.
            //
            // MinoBoss와 동일한 2단 판정: 보스 쪽에 기록된 선입력 버퍼(ParryBuffered)를
            // 먼저 확인하고, 실패하면 플레이어 컨트롤러의 즉시 판정(TryParry)으로 폴백한다.
            PlayerController2D pc = other.GetComponentInParent<PlayerController2D>();
            bool parried = parryBufferedCheck != null && parryBufferedCheck();
            if (!parried && pc != null && pc.TryParry(bossObject)) parried = true;
            if (parried)
            {
                if (pc != null) NAN2026.PlayerMana.RewardParry(pc);
                hasResolved = true;
                onParried?.Invoke();
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