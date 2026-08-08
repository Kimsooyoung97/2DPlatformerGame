using UnityEngine;

namespace NAN2026.Showroom
{
    // 소환수 유도탄. 보스 주변을 떠다니다 LaunchHoming 시 플레이어를 추적한다.
    // BossOrb 상속 → 기존 패링 시스템이 자동 감지. 패링되면 OnParried로 보스에 통지.
    public class SpiritMissile : NAN2026.BossOrb
    {
        private ExecutionerBoss boss;
        private Transform player;
        private Vector3 anchorOffset;
        private Sprite[] appearFrames;
        private Sprite[] idleFrames;
        private int damage;
        private SpriteRenderer sr;
        private float t;
        private bool homing;
        private Vector2 vel = Vector2.left;
        private float homingSpeed;
        private const float TurnRate = 4.5f;

        public void Init(ExecutionerBoss owner, Transform target, Vector3 offset, Sprite[] appear, Sprite[] idle, int dmg)
        {
            boss = owner; player = target; anchorOffset = offset;
            appearFrames = appear; idleFrames = idle; damage = dmg;
            sr = GetComponent<SpriteRenderer>();
            Launch(-1f, 0f, 9999f);
        }

        public void LaunchHoming(float s)
        {
            homing = true;
            homingSpeed = s;
            Launch(-1f, 0f, 8f);
            if (player != null)
                vel = ((player.position + Vector3.up * 0.7f) - transform.position).normalized;
        }

        protected override void Tick()
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                if (appearFrames != null && appearFrames.Length > 0 && t < appearFrames.Length / 10f)
                    sr.sprite = appearFrames[Mathf.Min((int)(t * 10f), appearFrames.Length - 1)];
                else if (idleFrames != null && idleFrames.Length > 0)
                    sr.sprite = idleFrames[(int)(t * 8f) % idleFrames.Length];
            }

            if (!homing)
            {
                if (boss != null)
                    transform.position = boss.transform.position + anchorOffset
                        + Vector3.up * (Mathf.Sin(t * 3f + anchorOffset.x) * 0.18f);
                return;
            }
            if (player != null)
            {
                Vector2 desired = ((player.position + Vector3.up * 0.7f) - transform.position).normalized;
                vel = ((Vector2)Vector3.RotateTowards(vel, desired, TurnRate * Time.deltaTime, 1f)).normalized;
            }
            transform.position += (Vector3)(vel * homingSpeed * Time.deltaTime);
            if (sr != null) sr.flipX = vel.x > 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!homing) return;
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph == null) return;
            ph.TakeDamage(damage);
            Destroy(gameObject);
        }

        private void OnParried()
        {
            if (boss != null) boss.RegisterParry();
        }
    }
}
