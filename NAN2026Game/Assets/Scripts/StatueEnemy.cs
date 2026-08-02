using System.Collections;
using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    // 기사석상: 잠든 석상 → 접근 시 각성 → 추적 → 내려찍기. HP3, Slash 피격.
    public class StatueEnemy : MonoBehaviour
    {
        [SerializeField] private StatueConfig config;
        [SerializeField] private Collider2D body;
        [SerializeField] private Collider2D hitbox;
        [SerializeField] private ParticleSystem dust;

        private Animator anim;
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private Transform player;
        private PlayerHealth playerHealth;
        private int state;
        private float timer;
        private float attackElapsed;
        private int hp;
        private bool hitApplied;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            hp = config.maxHp;
            state = StatueLogic.Dormant;
            body.enabled = false;
            hitbox.enabled = false;
            anim.Play("Awaken", 0, 0f);
            anim.speed = 0f; // 석상 정지 (각성 1프레임)
        }

        private void Start()
        {
            var pc = FindFirstObjectByType<PlayerController2D>();
            if (pc != null)
            {
                player = pc.transform;
                playerHealth = pc.GetComponent<PlayerHealth>();
            }
        }

        private void FixedUpdate()
        {
            if (player == null || state == StatueLogic.Dead) return;
            float dx = player.position.x - transform.position.x;
            float dist = Vector2.Distance(player.position, transform.position);
            timer -= Time.fixedDeltaTime;
            int next = StatueLogic.Next(state, dist, config.awakenRange, config.attackRange, timer <= 0f);
            if (next != state) Enter(next);
            if (state >= StatueLogic.Idle && state != StatueLogic.Attack)
                sr.flipX = StatueLogic.FaceLeft(dx);
            if (state == StatueLogic.Chase)
                rb.linearVelocity = new Vector2(Mathf.Sign(dx) * config.moveSpeed, rb.linearVelocity.y);
            else if (state != StatueLogic.Dormant)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (state == StatueLogic.Attack)
            {
                attackElapsed += Time.fixedDeltaTime;
                bool open = StatueLogic.HitboxOpen(attackElapsed, config.hitboxStart, config.hitboxEnd);
                hitbox.enabled = open;
                if (open && !hitApplied && playerHealth != null && hitbox.IsTouching(playerHealth.GetComponent<Collider2D>()))
                {
                    playerHealth.TakeDamage(config.damage);
                    hitApplied = true;
                }
            }
        }

        private void Enter(int next)
        {
            state = next;
            switch (next)
            {
                case StatueLogic.Awakening:
                    anim.speed = 1f;
                    anim.Play("Awaken", 0, 0f);
                    timer = config.awakenDuration;
                    body.enabled = true;
                    break;
                case StatueLogic.Idle:
                    if (dust != null) dust.Play();
                    StartCoroutine(WhiteFlash());
                    CameraShake();
                    anim.Play("Idle");
                    timer = config.idlePauseAfterAwaken;
                    break;
                case StatueLogic.Chase:
                    anim.Play("Walk");
                    break;
                case StatueLogic.Attack:
                    anim.Play("Slam", 0, 0f);
                    timer = config.slamDuration;
                    attackElapsed = 0f;
                    hitApplied = false;
                    break;
                case StatueLogic.Cooldown:
                    hitbox.enabled = false;
                    anim.Play("Idle");
                    timer = config.attackCooldown;
                    break;
            }
        }

        private void CameraShake()
        {
            var src = GetComponent("CinemachineImpulseSource") as MonoBehaviour;
            if (src != null)
            {
                var m = src.GetType().GetMethod("GenerateImpulse", new System.Type[0]);
                if (m != null) m.Invoke(src, null);
            }
        }

        private void OnTriggerEnter2D(Collider2D c)
        {
            if (state < StatueLogic.Idle || state == StatueLogic.Dead) return;
            if (!c.gameObject.name.Contains("Slash")) return;
            hp -= 1;
            if (hp <= 0) { Die(); return; }
            StartCoroutine(WhiteFlash());
        }

        private void Die()
        {
            state = StatueLogic.Dead;
            rb.linearVelocity = Vector2.zero;
            body.enabled = false;
            hitbox.enabled = false;
            anim.Play("Death", 0, 0f);
        }

        private IEnumerator WhiteFlash()
        {
            for (int i = 0; i < config.hitBlinkCount; i++)
            {
                sr.color = new Color(1f, 1f, 1f, 0.35f);
                yield return new WaitForSeconds(config.hitBlinkInterval);
                sr.color = Color.white;
                yield return new WaitForSeconds(config.hitBlinkInterval);
            }
        }
    }
}
