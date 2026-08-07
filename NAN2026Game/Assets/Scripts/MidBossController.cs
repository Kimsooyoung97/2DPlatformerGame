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

        [Header("MidBoss 자식으로 미리 배치해둔 근접 판정용 콜라이더")]
        [SerializeField] private GameObject normalHitboxObject;
        [SerializeField] private GameObject fireHitboxObject;
        [SerializeField] private GameObject wheelHitboxObject;
        [SerializeField] private GameObject bombHitboxObject;
        [SerializeField] private Transform[] childObjects;
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

        /// <summary>sr.flipX를 실제로 바꿀 때만 이 경로로 설정한다 — 값이 그대로면
        /// 아무것도 안 하고, 실제로 바뀔 때만 FlipHitBox()를 호출한다(값이 안 바뀌는데도
        /// 매 프레임 히트박스를 뒤집으면 위치가 계속 왔다갔다 하므로 반드시 변화 시에만).</summary>
        private void SetFacing(bool flipX)
        {
            if (sr == null) return;
            if (sr.flipX == flipX) return;
            sr.flipX = flipX;
            FlipHitBox();
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

            SetFacing(dx < 0f);

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

        private void FlipHitBox()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 childPos = childObjects[i].localPosition;
                childPos.x *= -1;
                childObjects[i].localPosition = childPos;
            }
        }
        private IEnumerator DoJump()
        {
            busy = true;
            if (player != null && sr != null)
            {
                float dx = player.position.x - transform.position.x;
                SetFacing(dx < 0f);
            }
            if (anim != null) anim.SetTrigger("Jump");
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, config.jumpVelocity);
            yield return new WaitForSeconds(config.jumpAnimLength);
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
                SetFacing(lockedAimDir.x < 0f);
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

        // 애니메이션 클립이 windup(혹은 windup+틱)보다 길면, 그 차이만큼 더 기다려서
        // 클립이 완전히 끝날 때까지 busy(=방향 고정)를 유지한다. 안 그러면 판정은
        // 끝났는데 애니메이션은 아직 재생 중인 구간에서 방향이 바뀌어 보인다.
        private IEnumerator HoldForRemainingAnim(float elapsed, float animLength)
        {
            float remaining = animLength - elapsed;
            if (remaining > 0f) yield return new WaitForSeconds(remaining);
        }

        private IEnumerator DoNormalAttack()
        {
            if (anim != null) anim.SetTrigger("NormalAttack");
            yield return new WaitForSeconds(config.normalAttackWindup);
            SpawnMeleeHitbox(config.normalAttackDamage, 0);
            yield return HoldForRemainingAnim(config.normalAttackWindup, config.normalAttackAnimLength);
        }

        private IEnumerator DoFireAttack()
        {
            // 검에 불 붙여 앞을 내려찍는 근접기 — 원거리 구체 아님.
            if (anim != null) anim.SetTrigger("FireAttack");
            yield return new WaitForSeconds(config.fireAttackWindup);
            SpawnMeleeHitbox(config.fireAttackDamage, 1);
            nextFireAttackTime = Time.time + config.fireAttackCooldown;
            yield return HoldForRemainingAnim(config.fireAttackWindup, config.fireAttackAnimLength);
        }

        private IEnumerator DoFireBomb()
        {
            // 검을 아래에서 위로 쳐올리며 앞에 폭발 이펙트가 나는 근접기 — 원거리 구체 아님.
            if (anim != null) anim.SetTrigger("FireBomb");
            yield return new WaitForSeconds(config.fireBombWindup);
            SpawnMeleeHitbox(config.fireBombDamage, 2);
            nextFireBombTime = Time.time + config.fireBombCooldown;
            yield return HoldForRemainingAnim(config.fireBombWindup, config.fireBombAnimLength);
        }

        private IEnumerator DoWheelAttack()
        {
            if (anim != null) anim.SetTrigger("WheelAttack");
            yield return new WaitForSeconds(config.wheelAttackWindup);
            SpawnMeleeHitbox(config.wheelAttackDamagePerTick, 3);
            yield return new WaitForSeconds(config.wheelAttackTickInterval);
            SpawnMeleeHitbox(config.wheelAttackDamagePerTick, 3);
            nextWheelAttackTime = Time.time + config.wheelAttackCooldown;
            yield return HoldForRemainingAnim(config.wheelAttackWindup + config.wheelAttackTickInterval, config.wheelAttackAnimLength);
        }

        // 동적 생성 대신, MidBoss 자식으로 미리 배치해둔 4개(Normal/Fire/Wheel/Bomb)
        // 오브젝트에 MidBossMeleeHitbox를 붙였다가(Init 실행) 판정 시간이 지나면
        // 컴포넌트만 떼어낸다 — 오브젝트 자체(콜라이더 배치)는 그대로 남아 재사용된다.
        private void SpawnMeleeHitbox(int damage, int skillnum)
        {
            StartCoroutine(SpawnMeleeHitboxRoutine(damage, skillnum));
        }

        private IEnumerator SpawnMeleeHitboxRoutine(int damage, int skillnum)
        {
            GameObject target = null;
            float lifeTime = 0f;
            switch (skillnum)
            {
                case 0:
                    target = normalHitboxObject;
                    lifeTime = config.NormalAttackHitboxLifetime/6;
                    break;
                case 1: 
                    target = fireHitboxObject;
                    lifeTime = config.FireAttackHitboxLifetime /6;
                    break;
                case 2: 
                    target = bombHitboxObject;
                    lifeTime = config.FireBombHitboxLifetime/6;
                    break;
                case 3: 
                    target = wheelHitboxObject;
                    lifeTime = config.WheelAttackHitboxLifetime/6;
                    break;
            }
            if (target == null) yield break;



            // 2. 대기 후 컴포넌트 추가 및 Init 실행
            // (대기하는 동안 target 오브젝트가 파괴되었을 수도 있으므로 파괴 여부 체크)
            if (target != null)
            {
                MidBossMeleeHitbox hitbox = target.AddComponent<MidBossMeleeHitbox>();
                hitbox.Init(damage, gameObject);

                // ※ 필요 시 일정 시간 후 파괴하는 코드도 추가 가능합니다.
                Destroy(hitbox, lifeTime);
            }

        }
    }
}
