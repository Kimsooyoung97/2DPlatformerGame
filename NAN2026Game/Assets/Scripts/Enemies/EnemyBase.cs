using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    /// 잡몹 공통 뼈대. 상태 판단은 EnemyStateLogic(순수)에 위임하고
    /// 여기서는 스프라이트 재생·이동·판정만 담당한다.
    public abstract class EnemyBase : MonoBehaviour, IPlayerDamageable
    {
        public EnemyConfig config;
        public Sprite[] idleFrames, walkFrames, attackFrames, hurtFrames, deathFrames;

        protected int state = EnemyStateLogic.Idle;
        protected float stateT;
        protected int hits;
        protected float nextAtk;
        protected bool dealtThisSwing;
        protected SpriteRenderer sr;
        protected Transform player;
        private Coroutine flashCo;
        private float spawnY;              // 배치 높이를 접지 기준으로 (config.groundY 고정 시 다층 배치 불가)
        private static readonly System.Collections.Generic.List<EnemyBase> All = new System.Collections.Generic.List<EnemyBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { All.Clear(); }   // DisableDomainReload 대응

        protected virtual void Start()
        {
            if (config == null) { Debug.LogError("[" + name + "] EnemyConfig 미배선", this); enabled = false; return; }
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.useFullKinematicContacts = true; // FAIL#6
            player = PlayerLocator.FindTransform();
            spawnY = transform.position.y;
            nextAtk = Time.time + EnemyStateLogic.InitialDelay(config.fireStagger, Random.value); // 첫 발 산개
            if (!All.Contains(this)) All.Add(this);
            SetState(EnemyStateLogic.Idle);
        }

        protected virtual void OnDestroy() { All.Remove(this); }

        protected virtual void Update()
        {
            if (config == null) return;
            stateT += Time.deltaTime;
            if (config.snapToGround)
            {
                var p = transform.position;
                if (!Mathf.Approximately(p.y, spawnY)) transform.position = new Vector3(p.x, spawnY, p.z);
            }

            if (state == EnemyStateLogic.Death)
            {
                Anim(deathFrames, false);
                if (stateT >= config.deathLinger) Destroy(gameObject);
                return;
            }
            if (state == EnemyStateLogic.Hurt)
            {
                Anim(hurtFrames, false);
                if (stateT >= config.hurtLock) SetState(EnemyStateLogic.Idle);
                return;
            }
            if (player == null) { player = PlayerLocator.FindTransform(); Anim(idleFrames, true); return; }

            float dx = Mathf.Abs(player.position.x - transform.position.x);
            float face = EnemyStateLogic.FaceSign(transform.position.x, player.position.x);
            if (state != EnemyStateLogic.Attack) sr.flipX = FlipFor(face);

            if (state == EnemyStateLogic.Attack) { DoAttack(dx, face); return; }

            int want = EnemyStateLogic.DecideWithHold(dx, config.aggroRange, config.attackRange, Time.time >= nextAtk);
            if (want == EnemyStateLogic.Attack) { SetState(EnemyStateLogic.Attack); return; }
            if (want == EnemyStateLogic.Walk && !BlockedAhead(face))
            {
                float step = EnemyStateLogic.MoveStep(dx, config.stopDistance, config.walkSpeed, Time.deltaTime);
                if (step > 0f)
                {
                    transform.position += new Vector3(face * step, 0f, 0f);
                    Anim(walkFrames, true);
                    return;
                }
            }
            Anim(idleFrames, true);
        }

        /// 진행 방향 앞에 같은 종류의 동료가 separation 안에 있으면 멈춘다(겹침 방지).
        protected bool BlockedAhead(float moveSign)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == null || o == this) continue;
                if (o.state == EnemyStateLogic.Death) continue;
                if (EnemyStateLogic.BlockedByNeighbor(transform.position.x, o.transform.position.x, moveSign, config.separation))
                    return true;
            }
            return false;
        }

        /// 다음 공격까지의 대기. 쿨다운에 편차를 줘 개체 간 동기화를 깬다.
        protected float NextAttackAt()
        {
            return Time.time + EnemyStateLogic.JitteredCooldown(config.attackCooldown, config.cooldownJitter, Random.value);
        }

        /// 시트 기본 바라보는 방향에 따라 반전 규칙이 다르다.
        protected abstract bool FlipFor(float face);

        /// 공격 진행. 타격 시간창에서 ResolveHit(), 발사형은 오버라이드.
        protected virtual void DoAttack(float dx, float face)
        {
            Anim(attackFrames, false);
            float frac = stateT / config.attackDur;
            if (!dealtThisSwing && BossRangeLogic.WindowOpen(frac, config.hitWinS, config.hitWinE)
                && BossRangeLogic.InHitBand(transform.position.x, player.position.x, config.attackRange, face, config.frontDeadZone))
            {
                dealtThisSwing = true;
                player.SendMessage("TakeDamage", (float)config.damage, SendMessageOptions.DontRequireReceiver);
            }
            if (frac >= 1f) { nextAtk = NextAttackAt(); SetState(EnemyStateLogic.Idle); }
        }

        public void TakeDamage(int amount)
        {
            if (state == EnemyStateLogic.Death) return;
            hits++;
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRed());
            if (EnemyStateLogic.IsDead(hits, config.hitsToDie)) { SetState(EnemyStateLogic.Death); return; }
            SetState(EnemyStateLogic.Hurt);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 0.4f, 0.4f);
            yield return new WaitForSeconds(config.hitFlash);
            if (sr != null) sr.color = Color.white;
            flashCo = null;
        }

        protected virtual void SetState(int s) { state = s; stateT = 0f; dealtThisSwing = false; }

        protected void Anim(Sprite[] arr, bool loop)
        {
            if (arr == null || arr.Length == 0 || sr == null) return;
            sr.sprite = arr[EnemyStateLogic.AnimIndex(stateT, config.fps, arr.Length, loop)];
        }
    }
}
