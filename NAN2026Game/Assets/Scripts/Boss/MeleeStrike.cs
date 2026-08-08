using UnityEngine;

namespace NAN2026.Showroom
{
    // 보스 근접 공격 판정. 짧게 존재하는 투명 히트박스.
    // BossOrb 상속 → 패링 가능. 패링되면 보스에 통지(카운트).
    public class MeleeStrike : NAN2026.BossOrb
    {
        private ExecutionerBoss boss;
        private int damage;
        private bool consumed;

        public void Init(ExecutionerBoss owner, int dmg, float life)
        {
            boss = owner; damage = dmg;
            Launch(-1f, 0f, life);
        }

        protected override void Tick() { }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph == null) return;
            consumed = true;
            ph.TakeDamage(damage);
            Destroy(gameObject);
        }

        private void OnParried()
        {
            if (boss != null) boss.RegisterParry();
        }
    }
}
