using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // 파이어나이트 미들보스: Idle/Walk/NormalAttack/FireAttack/FireBomb/WheelAttack/Hitted/Death + Windup/Groggy
    // Demon/Mino와 같은 형식: Sprite[] 직접 재생(Animator 미사용), state int + SetState, 공격별 개별
    // windup·쿨타임, 패링 5회 그로기(버스트). 수치는 전부 이 컴포넌트가 아니라 Config가 소유한다.
    // 근접 판정: DemonBoss와 동일하게 물리 콜라이더 없이 거리(dx)+프레임 구간으로 직접 판정한다.
    // (이전엔 씬에 미리 배치된 히트박스 오브젝트를 썼으나, 사용자 명시적 지시로 그 오브젝트들을
    // 직접 삭제하고 이 방식으로 전환함 — "수동 배치 오브젝트 삭제 금지" 규칙의 명시적 예외.)
    public class MidBoss_FireKnight : MonoBehaviour, IParryReflector
    {
        public MidBossFireKnightConfig config;
        public Sprite[] idleF, walkF, normalF, fireF, bombF, wheelF, hitF, deathF;

        private SpriteRenderer sr;
        private Transform player;
        private Component controller;
        private System.Reflection.MethodInfo tryParry;

        private int hp;
        private int state; // 0 idle 1 walk 2 normal 3 fire 4 bomb 5 wheel 6 hit 7 death 8 groggy 9 windup
        private float animT, stateT;
        private float nextNormal, nextFire, nextBomb, nextWheel;
        private int pendingAttack;   // windup 종료 후 진입할 state
        private float curWindupDur;
        private bool dealtThisSwing;         // Normal/Fire/Bomb 공용(판정 창 1개)
        private bool[] wheelSwingResolved = new bool[2]; // Wheel은 판정 창 2개
        private Sprite[] cur;
        private float curFps;
        private int parryCount;
        private Coroutine flashCo;
        private Coroutine sparkleCo;
        private Coroutine dashCo;
        private GameObject groggyFx;
        private TextMesh groggyPips;
        private GameObject burstMsg;
        private SpriteRenderer playerSr;
        private float lastParryPress = -999f;
        private float lastConsumed = -999f;

        private bool ParryBuffered()
        {
            if (Time.time - lastParryPress <= config.parryBuffer && lastParryPress > lastConsumed)
            { lastConsumed = lastParryPress; return true; }
            return false;
        }

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            hp = config.maxHp;
            var rbSelf = GetComponent<Rigidbody2D>();
            if (rbSelf != null) rbSelf.useFullKinematicContacts = true; // Kinematic끼리 트리거 이벤트 보장

            var p = PlayerLocator.Find();
            if (p != null)
            {
                player = p.transform;
                foreach (var mb in p.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }

            BuildGroggyPips();
            SetState(0);
        }

        public bool TryParry(GameObject attacker) => false; // 이 보스는 패링 판정을 직접 소유하지 않는다(플레이어 쪽에서 판정)

        private void SetState(int s)
        {
            state = s; animT = 0f; stateT = 0f;
            dealtThisSwing = false;
            wheelSwingResolved[0] = false; wheelSwingResolved[1] = false;
            cur = s == 0 ? idleF
                : s == 1 ? walkF
                : s == 2 ? normalF
                : s == 3 ? fireF
                : s == 4 ? bombF
                : s == 5 ? wheelF
                : s == 6 ? hitF
                : s == 8 ? hitF    // groggy: 별도 시트 없이 피격 프레임 재사용
                : s == 9 ? idleF   // windup: 별도 시트 없이 idle 프레임 유지
                : deathF;
            curFps = s == 0 ? config.fpsIdle
                : s == 1 ? config.fpsWalk
                : s == 2 ? config.fpsNormal
                : s == 3 ? config.fpsFire
                : s == 4 ? config.fpsBomb
                : s == 5 ? config.fpsWheel
                : s == 8 ? config.fpsIdle
                : s == 9 ? config.fpsIdle
                : s == 7 ? config.fpsDeath
                : config.fpsHit;
            if (s == 8) { BeginGroggyFx(); BeginBurst(); } else { EndGroggyFx(); EndBurst(); }
        }

        private void BuildGroggyPips()
        {
            var go = new GameObject("GroggyPips");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, config.groggyFxOffsetY + 0.7f, 0f);
            groggyPips = go.AddComponent<TextMesh>();
            groggyPips.fontSize = 40; groggyPips.characterSize = 0.07f;
            groggyPips.anchor = TextAnchor.MiddleCenter;
            groggyPips.color = new Color(1f, 0.55f, 0.15f);
            go.GetComponent<MeshRenderer>().sortingOrder = 899;
            RefreshGroggyPips();
        }

        private void RefreshGroggyPips()
        {
            if (groggyPips == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < config.groggyNeed; i++) sb.Append(i < parryCount ? '\u25c6' : '\u25c7');
            groggyPips.text = sb.ToString();
        }

        private void BeginBurst()
        {
            PlayerController2D.AttackSpeedMul = config.burstAtkSpeedMul;
            burstMsg = new GameObject("BurstMsg");
            burstMsg.transform.position = (player != null ? player.position : transform.position) + Vector3.up * 2.6f;
            var tm = burstMsg.AddComponent<TextMesh>();
            tm.text = "Z 연타! 공격 찬스!";
            tm.fontSize = 52; tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.85f, 0.2f);
            burstMsg.GetComponent<MeshRenderer>().sortingOrder = 950;
            if (player != null) playerSr = player.GetComponent<SpriteRenderer>();
            sparkleCo = StartCoroutine(SparkleLoop());
        }

        private void EndBurst()
        {
            PlayerController2D.AttackSpeedMul = 1f;
            if (burstMsg != null) Destroy(burstMsg);
            if (sparkleCo != null) StopCoroutine(sparkleCo);
            if (playerSr != null) playerSr.color = Color.white;
        }

        private System.Collections.IEnumerator SparkleLoop()
        {
            float t0 = Time.time;
            while (state == 8)
            {
                if (playerSr != null)
                {
                    float g = 0.75f + 0.25f * Mathf.Sin((Time.time - t0) * 10f);
                    playerSr.color = new Color(1f, g, 0.55f + 0.45f * g);
                }
                var star = new GameObject("BurstStar");
                star.transform.position = (player != null ? player.position : transform.position)
                    + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1.6f), 0f);
                var st = star.AddComponent<TextMesh>();
                st.text = "\u2726";
                st.fontSize = 36; st.characterSize = 0.06f;
                st.anchor = TextAnchor.MiddleCenter;
                st.color = new Color(1f, 0.95f, 0.4f);
                star.GetComponent<MeshRenderer>().sortingOrder = 940;
                star.AddComponent<PopupFloater>().Init(0.9f, 0.55f);
                yield return new WaitForSeconds(config.sparkleInterval);
            }
        }

        private System.Collections.IEnumerator DashToBoss()
        {
            PlayerController2D.InputLocked = true;
            var rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
            float side = player.position.x < transform.position.x ? -1f : 1f;
            Vector3 target = transform.position + new Vector3(side * config.burstDashStopX, 0f, 0f);
            target.y = player.position.y;
            while (state == 8 && Vector2.Distance(player.position, target) > 0.08f)
            {
                player.position = Vector3.MoveTowards(player.position, target, config.burstDashSpeed * Time.deltaTime);
                if (rb != null) rb.linearVelocity = Vector2.zero;
                yield return null;
            }
            PlayerController2D.InputLocked = false;
            dashCo = null;
        }

        private void BeginGroggyFx()
        {
            EndGroggyFx();
            groggyFx = new GameObject("GroggyFx");
            groggyFx.transform.SetParent(transform, false);
            groggyFx.transform.localPosition = new Vector3(0f, config.groggyFxOffsetY, 0f);
            var tm = groggyFx.AddComponent<TextMesh>();
            tm.text = "\u2605 \u2605 \u2605";
            tm.fontSize = 56; tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.9f, 0.2f);
            groggyFx.GetComponent<MeshRenderer>().sortingOrder = 901;
        }

        private void EndGroggyFx()
        {
            if (groggyFx != null) Destroy(groggyFx);
        }

        public void TakeDamage(int dmg)
        {
            if (state == 7) return; // death
            hp -= 1;
            HitFeedback();
            if (hp <= 0) { SetState(7); return; }
            bool attacking = state == 2 || state == 3 || state == 4 || state == 5; // 공격 판정·모션 중엔 경직 없음
            if (state != 8 && !attacking) SetState(6);
        }

        private void HitFeedback()
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRed());
            var pop = new GameObject("BossHpPopup");
            pop.transform.position = transform.position + Vector3.up * (config.groggyFxOffsetY + 1.4f);
            var tm = pop.AddComponent<TextMesh>();
            tm.text = "HP " + Mathf.Max(0, hp) + " / " + config.maxHp;
            tm.fontSize = 46; tm.characterSize = 0.075f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.3f, 0.25f);
            pop.GetComponent<MeshRenderer>().sortingOrder = 902;
            pop.AddComponent<PopupFloater>().Init(1.0f, 0.7f);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 0.35f, 0.35f);
            yield return new WaitForSeconds(0.12f);
            sr.color = Color.white;
            flashCo = null;
        }

        private void SetFacing(bool flipX)
        {
            if (sr == null) return;
            sr.flipX = flipX;
        }

        private void Update()
        {
            if (config == null || cur == null || cur.Length == 0) return;

            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) lastParryPress = Time.time;

            animT += Time.deltaTime * curFps;
            stateT += Time.deltaTime;
            bool loop = state == 0 || state == 1;
            int idx = loop ? (int)animT % cur.Length : Mathf.Min((int)animT, cur.Length - 1);
            sr.sprite = cur[idx];

            if (groggyFx != null) groggyFx.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 6f) * 14f);

            // 공격·windup·groggy·death 중엔 방향 고정 — windup 시작 시점에 확정된 방향 유지
            bool facingLocked = state == 2 || state == 3 || state == 4 || state == 5 || state == 7 || state == 8 || state == 9;
            if (player != null && !facingLocked) SetFacing(player.position.x < transform.position.x);

            if (state == 7) { if ((int)animT >= cur.Length - 1) enabled = false; return; }
            if (player == null) return;

            if (state == 9) { DoWindup(); return; }

            float dx = Mathf.Abs(player.position.x - transform.position.x);

            if (state == 0)
            {
                if (dx <= config.aggroRange && dx > config.attackRange) SetState(1);
                else if (dx <= config.attackRange) TryBeginAttack();
            }
            else if (state == 1)
            {
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                transform.position += new Vector3(dir * config.walkSpeed * Time.deltaTime, 0f, 0f);
                if (dx <= config.attackRange) TryBeginAttack();
                else if (dx > config.aggroRange) SetState(0);
            }
            else if (state == 2) DoNormalAttack(dx);
            else if (state == 3) DoFireAttack(dx);
            else if (state == 4) DoFireBomb(dx);
            else if (state == 5) DoWheelAttack(dx);
            else if (state == 6) { if ((int)animT >= cur.Length) SetState(0); }
            else if (state == 8) DoGroggy(dx);
        }

        // 쿨타임이 돌아온 공격 우선(랜덤 없음). 우선순위: Normal > Fire > Bomb > Wheel — 임의 지정.
        private void TryBeginAttack()
        {
            bool normalReady = Time.time >= nextNormal;
            bool fireReady = Time.time >= nextFire;
            bool bombReady = Time.time >= nextBomb;
            bool wheelReady = Time.time >= nextWheel;
            if (!normalReady && !fireReady && !bombReady && !wheelReady) return;

            if (player != null) SetFacing(player.position.x < transform.position.x);

            if (normalReady) BeginWindup(2, config.normalWindup);
            else if (fireReady) BeginWindup(3, config.fireWindup);
            else if (bombReady) BeginWindup(4, config.bombWindup);
            else BeginWindup(5, config.wheelWindup);
        }

        // 공격 예열: idle 프레임 유지한 채 색상 플래시로 경고, 지속 후 실제 공격 state 진입
        private void BeginWindup(int attackState, float windupDur)
        {
            pendingAttack = attackState;
            curWindupDur = windupDur;
            SetState(9);
        }

        private void DoWindup()
        {
            if (curWindupDur > 0f)
            {
                float pulse = Mathf.PingPong(stateT * config.windupFlashSpeed, 1f);
                sr.color = Color.Lerp(Color.white, config.windupFlashColor, pulse);
            }
            if (stateT >= curWindupDur)
            {
                sr.color = Color.white;
                SetState(pendingAttack);
            }
        }

        // DemonBoss 방식: 거리(dx) + 프레임 구간으로 직접 판정. 물리 히트박스 없음.
        private void DoNormalAttack(float dx)
        {
            int idx = Mathf.Min((int)animT, cur.Length - 1);
            bool inWin = idx >= config.normalWinStart && idx <= config.normalWinEnd;
            if (!dealtThisSwing && inWin && dx <= config.normalHitReach)
            {
                dealtThisSwing = true;
                ResolveMeleeHit(config.normalDamage);
            }
            if ((int)animT >= cur.Length) { nextNormal = Time.time + config.normalCooldown; SetState(0); }
        }

        private void DoFireAttack(float dx)
        {
            int idx = Mathf.Min((int)animT, cur.Length - 1);
            bool inWin = idx >= config.fireWinStart && idx <= config.fireWinEnd;
            if (!dealtThisSwing && inWin && dx <= config.fireHitReach)
            {
                dealtThisSwing = true;
                ResolveMeleeHit(config.fireDamage);
            }
            if ((int)animT >= cur.Length) { nextFire = Time.time + config.fireCooldown; SetState(0); }
        }

        private void DoFireBomb(float dx)
        {
            int idx = Mathf.Min((int)animT, cur.Length - 1);
            bool inWin = idx >= config.bombWinStart && idx <= config.bombWinEnd;
            if (!dealtThisSwing && inWin && dx <= config.bombHitReach)
            {
                dealtThisSwing = true;
                ResolveMeleeHit(config.bombDamage);
            }
            if ((int)animT >= cur.Length) { nextBomb = Time.time + config.bombCooldown; SetState(0); }
        }

        private void DoWheelAttack(float dx)
        {
            int idx = Mathf.Min((int)animT, cur.Length - 1);
            bool inWin1 = idx >= config.wheelWin1Start && idx <= config.wheelWin1End;
            bool inWin2 = idx >= config.wheelWin2Start && idx <= config.wheelWin2End;
            if (!wheelSwingResolved[0] && inWin1 && dx <= config.wheelHitReach)
            {
                wheelSwingResolved[0] = true;
                ResolveMeleeHit(config.wheelDamagePerTick);
            }
            if (!wheelSwingResolved[1] && inWin2 && dx <= config.wheelHitReach)
            {
                wheelSwingResolved[1] = true;
                ResolveMeleeHit(config.wheelDamagePerTick);
            }
            if ((int)animT >= cur.Length) { nextWheel = Time.time + config.wheelCooldown; SetState(0); }
        }

        // 판정 창 안에서 사거리까지 맞았을 때 공통으로 호출 — 패링(버퍼 우선, 리플렉션 폴백) 성공 시
        // RegisterParrySuccess(그로기 카운트), 실패 시 플레이어에게 데미지. DemonBoss.ResolveHit()와 동일 패턴.
        private void ResolveMeleeHit(int damage)
        {
            bool parried = ParryBuffered();
            if (!parried && controller != null && tryParry != null)
            {
                object r = tryParry.Invoke(controller, new object[] { gameObject });
                parried = r is bool && (bool)r;
            }
            if (parried)
            {
                if (player != null) PlayerMana.RewardParry(player);
                RegisterParrySuccess();
            }
            else if (player != null)
            {
                player.SendMessage("TakeDamage", (float)damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void DoGroggy(float dx)
        {
            if (burstMsg != null && player != null)
                burstMsg.transform.position = player.position + Vector3.up * 2.6f;
            var kb = Keyboard.current;
            if (kb != null && kb.zKey.wasPressedThisFrame && dashCo == null && dx > config.burstDashStopX + 0.5f)
                dashCo = StartCoroutine(DashToBoss());
            if (stateT >= config.groggyTime)
            {
                nextNormal = nextFire = nextBomb = nextWheel = Time.time + config.groggyExitCooldown;
                SetState(0);
            }
        }

        // 근접 판정(ResolveMeleeHit)이 패링 성공을 알려올 때 호출 — 그로기 카운터 공유
        public void RegisterParrySuccess()
        {
            if (config.clashConfig != null && player != null)
                ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, config.clashConfig);
            parryCount++;
            RefreshGroggyPips();
            if (parryCount >= config.groggyNeed)
            {
                parryCount = 0; RefreshGroggyPips();
                nextNormal = nextFire = nextBomb = nextWheel = Time.time + config.groggyExitCooldown;
                SetState(8);
            }
        }
    }
}
