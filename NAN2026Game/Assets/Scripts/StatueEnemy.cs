using System.Collections;
using UnityEngine;
using NAN2026.Core;
using UnityEngine.InputSystem;

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
        private bool forceAwaken;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            hp = config.maxHp;
            state = StatueLogic.Dormant;
            body.enabled = false;
            hitbox.enabled = false;
            rb.bodyType = RigidbodyType2D.Kinematic; // 잠듦: 콜라이더 없음 + 중력 무시 (추락 방지)
            anim.Play("Awaken", 0, 0f);
            anim.speed = 0f; // 석상 정지 (각성 1프레임)
        }

        private void Start()
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null)
            {
                player = pc.transform;
                playerHealth = pc.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            // 공주 보스와 동일: 마우스 오른쪽 버튼으로 강제 각성 (근접 감지와 병행)
            if (state == StatueLogic.Dormant && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                forceAwaken = true;
        }

        private void FixedUpdate()
        {
            if (player == null || state == StatueLogic.Dead) return;
            float dx = player.position.x - transform.position.x;
            float dist = Vector2.Distance(player.position, transform.position);
            if (forceAwaken && state == StatueLogic.Dormant) dist = 0f;
            timer -= Time.fixedDeltaTime;
            int next = StatueLogic.Next(state, dist, config.awakenRange, config.attackRange, timer <= 0f);
            if (next != state) Enter(next);
            if (state >= StatueLogic.Idle && state != StatueLogic.Attack)
                sr.flipX = StatueLogic.FaceLeft(dx);
            if (state == StatueLogic.Chase)
            {
                float dir = Mathf.Sign(dx);
                // 낭떠러지 가드: 전방 발끝 아래에 지형 없으면 정지 (2층에서 걸어 떨어짐 방지)
                Vector2 probe = (Vector2)transform.position + new Vector2(dir * config.edgeProbeAhead, 0.1f);
                bool groundAhead = false;
                foreach (var hit in Physics2D.RaycastAll(probe, Vector2.down, config.edgeProbeDepth))
                {
                    if (hit.collider == null || hit.collider.isTrigger) continue;
                    if (hit.collider is UnityEngine.Tilemaps.TilemapCollider2D || hit.collider is CompositeCollider2D) { groundAhead = true; break; }
                }
                rb.linearVelocity = new Vector2(groundAhead ? dir * config.moveSpeed : 0f, rb.linearVelocity.y);
            }
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
                    rb.bodyType = RigidbodyType2D.Dynamic;
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
            rb.bodyType = RigidbodyType2D.Kinematic;
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
