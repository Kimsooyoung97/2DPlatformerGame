using System.Collections;
using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// MidBoss 신규 4패턴(NormalAttack/FireAttack/FireBomb/WheelAttack) + 추격 + 점프 +
    /// 사망 처리. 기존 MidBossAI(단일 SpAtk, 팀원 작성)는 그대로 두고 비활성화만 한다 —
    /// 지우지 않는다. MonsterController2D/MonsterAnimation(PixelFantasy 표준 EnemyAI 경로)에는
    /// 의존하지 않고, MidBoss가 이미 갖고 있던 개별 이름 상태 Animator를 직접 Trigger로 몬다.
    /// 체력은 NHNDemo.MonsterHealth를 그대로 사용한다(플레이어 공격 판정 코드가 전부 이 컴포넌트를
    /// 찾아서 데미지를 주기 때문 — 직접 만든 체력 시스템으로는 플레이어가 때릴 수 없다).
    /// </summary>
    [RequireComponent(typeof(NHNDemo.MonsterHealth))]
    public sealed class MidBossController : MonoBehaviour, IParryReflector
    {
        [SerializeField] private MidBossPatternConfig config;
        [SerializeField] private Transform player;

        private Animator anim;
        private SpriteRenderer sr;
        private NHNDemo.MonsterHealth health;

        private bool busy;
        private bool dead;
        private float nextFireAttackTime;
        private float nextFireBombTime;
        private float nextWheelAttackTime;
        private float nextPatternAllowedTime;
        private float heightGapTimer;
        private const float JumpConfirmDuration = 0.15f;
        // 공격 시작 순간의 조준 방향을 고정한다 — 윈드업 도중 플레이어가 반대편으로
        // 넘어가도 이미 시작된 공격은 처음 방향 그대로 나가야 자연스럽다.
        private Vector2 lockedAimDir = Vector2.right;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            health = GetComponent<NHNDemo.MonsterHealth>();
            if (health != null) health.OnDied += HandleDied;

            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) player = playerGO.transform;
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            dead = true;
            if (anim != null) anim.SetTrigger("Death");
        }

        public bool TryParry(GameObject attacker)
        {
            // 이 보스 자체는 패링 판정을 직접 소유하지 않는다(플레이어 쪽에서 판정).
            // 인터페이스는 SpikeProjectile 등이 발사자(owner)를 통해 패링 여부를 물어볼 때 쓰인다.
            return false;
        }

        private void Update()
        {
            if (config == null || player == null || dead) return;
            if (anim != null) anim.SetBool("IsMoving", false);
            if (busy) return;

            float dx = player.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            if (sr != null) sr.flipX = dx < 0f;

            // 점프 추격: 플레이어가 임계 높이 이상 위에 '유지'되고 있을 때만 점프로 취급한다.
            bool aboveNow = (player.position.y - transform.position.y) >= config.jumpYThreshold;
            heightGapTimer = aboveNow ? heightGapTimer + Time.deltaTime : 0f;
            if (heightGapTimer >= JumpConfirmDuration && dist <= config.aggroRange)
            {
                StartCoroutine(DoJump());
                return;
            }

            if (dist <= config.attackRange && Time.time >= nextPatternAllowedTime)
            {
                StartCoroutine(DoRandomPattern());
                return;
            }

            if (dist <= config.aggroRange)
            {
                if (anim != null) anim.SetBool("IsMoving", true);
                float dir = Mathf.Sign(dx);
                transform.position += new Vector3(dir * config.chaseSpeed * Time.deltaTime, 0f, 0f);
            }
        }

        private IEnumerator DoJump()
        {
            busy = true;
            if (anim != null) anim.SetTrigger("Jump");
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, config.jumpVelocity);
            yield return new WaitForSeconds(0.5f);
            busy = false;
        }

        private IEnumerator DoRandomPattern()
        {
            busy = true;

            // 공격 시작 시점의 방향을 고정: 스프라이트 반전과 원거리 조준 모두 이 값을 쓴다.
            if (player != null)
            {
                Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
                lockedAimDir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
                if (sr != null) sr.flipX = lockedAimDir.x < 0f;
            }

            var choices = new System.Collections.Generic.List<int> { 0 }; // NormalAttack은 항상 가능
            if (Time.time >= nextFireAttackTime) choices.Add(1);
            if (Time.time >= nextFireBombTime) choices.Add(2);
            if (Time.time >= nextWheelAttackTime) choices.Add(3);

            int pick = choices[Random.Range(0, choices.Count)];
            switch (pick)
            {
                case 0: yield return DoNormalAttack(); break;
                case 1: yield return DoFireAttack(); break;
                case 2: yield return DoFireBomb(); break;
                case 3: yield return DoWheelAttack(); break;
            }

            nextPatternAllowedTime = Time.time + config.minPatternGap;
            busy = false;
        }

        private IEnumerator DoNormalAttack()
        {
            if (anim != null) anim.SetTrigger("NormalAttack");
            yield return new WaitForSeconds(config.normalAttackWindup);
            TryHitMelee(config.normalAttackDamage, config.normalAttackReach);
        }

        private IEnumerator DoFireAttack()
        {
            if (anim != null) anim.SetTrigger("FireAttack");
            yield return new WaitForSeconds(config.fireAttackWindup);
            FireOrb(config.fireAttackDamage, config.fireAttackOrbSpeed, config.fireAttackSpawnHeight);
            nextFireAttackTime = Time.time + config.fireAttackCooldown;
        }

        private IEnumerator DoFireBomb()
        {
            if (anim != null) anim.SetTrigger("FireBomb");
            yield return new WaitForSeconds(config.fireBombWindup);
            FireOrb(config.fireBombDamage, config.fireBombOrbSpeed, config.fireBombSpawnHeight);
            nextFireBombTime = Time.time + config.fireBombCooldown;
        }

        private IEnumerator DoWheelAttack()
        {
            if (anim != null) anim.SetTrigger("WheelAttack");
            yield return new WaitForSeconds(config.wheelAttackWindup);
            TryHitMelee(config.wheelAttackDamagePerTick, config.wheelAttackReach);
            yield return new WaitForSeconds(config.wheelAttackTickInterval);
            TryHitMelee(config.wheelAttackDamagePerTick, config.wheelAttackReach);
            nextWheelAttackTime = Time.time + config.wheelAttackCooldown;
        }

        private void TryHitMelee(int damage, float reach)
        {
            if (player == null) return;
            float dist = Mathf.Abs(player.position.x - transform.position.x);
            if (dist > reach) return;

            PlayerController2D pc = player.GetComponentInParent<PlayerController2D>();
            if (pc != null && pc.TryParry(gameObject)) return; // 패링당하면 피해 없음

            PlayerHealth ph = player.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }

        private void FireOrb(int damage, float speed, float spawnHeight)
        {
            if (player == null) return;
            Vector3 spawnPos = transform.position + Vector3.up * spawnHeight;
            // 발사 순간 플레이어 위치를 다시 조준하지 않고, 공격 시작 시 고정해둔 방향을 쓴다.
            Vector2 dir = lockedAimDir;
            GameObject go = new GameObject("MidBossOrb");
            go.transform.position = spawnPos;
            SpikeProjectile proj = go.AddComponent<SpikeProjectile>();
            proj.Init(dir, speed, damage, health);
        }
    }
}
