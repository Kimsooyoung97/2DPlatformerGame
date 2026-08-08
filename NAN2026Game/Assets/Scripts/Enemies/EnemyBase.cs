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

        protected virtual void Start()
        {
            if (config == null) { Debug.LogError("[" + name + "] EnemyConfig 미배선", this); enabled = false; return; }
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.useFullKinematicContacts = true; // FAIL#6
            player = PlayerLocator.FindTransform();
            SetState(EnemyStateLogic.Idle);
        }

        protected virtual void Update()
        {
            if (config == null) return;
            stateT += Time.deltaTime;
            if (config.snapToGround)
            {
                var p = transform.position;
                if (!Mathf.Approximately(p.y, config.groundY)) transform.position = new Vector3(p.x, config.groundY, p.z);
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

            int want = EnemyStateLogic.Decide(dx, config.aggroRange, config.attackRange, Time.time >= nextAtk);
            if (want == EnemyStateLogic.Attack) { SetState(EnemyStateLogic.Attack); return; }
            if (want == EnemyStateLogic.Walk)
            {
                transform.position += new Vector3(face * config.walkSpeed * Time.deltaTime, 0f, 0f);
                Anim(walkFrames, true);
                return;
            }
            Anim(idleFrames, true);
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
            if (frac >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(EnemyStateLogic.Idle); }
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
