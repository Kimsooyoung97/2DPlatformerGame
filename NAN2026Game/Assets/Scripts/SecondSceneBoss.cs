using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

namespace NAN2026
{
    // 미노: idle/walk/atk(홀드·패링)/take_hit(항상)/groggy(패링 5회)/death(10타)
    public class SecondSceneBoss : MonoBehaviour
    {
        public MinoBossConfig config;
        public Sprite[] idleF, walkF, atk1F, atk2F, hitF, deathF;
        private SpriteRenderer sr;
        private Transform player;
        private Component controller;
        private MethodInfo tryParry;
        private int hp;
        private int state; // 0 idle 1 walk 2 attack 3 hit 4 death 5 groggy
        private float animT, stateT, nextAtk, holdT;
        private Sprite[] cur;
        private float curFps;
        private bool atkIs1, dealtThisSwing, holdDone;
        private int parryCount;
        private bool[] swingResolved = new bool[2];
        private Transform barFill;
        private float barFullW;
        private GameObject groggyFx;
        private float lastParryPress = -999f;
        private float lastConsumed = -999f;
        private bool ParryBuffered()
        {
            // 최근 buffer 내 새 입력이 있고 아직 소비 안 됐으면 성립 (일찍 눌러도 OK)
            if (Time.time - lastParryPress <= config.parryBuffer && lastParryPress > lastConsumed)
            { lastConsumed = lastParryPress; return true; }
            return false;
        }

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            hp = config.maxHp;
            var p = GameObject.Find("Player");
            if (p != null)
            {
                player = p.transform;
                foreach (var mb in p.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }
            BuildBar();
            SetState(0);
        }

        private void BuildBar()
        {
            if (config.barUnder == null) return;
            var root = new GameObject("HPBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, config.barOffsetY / Mathf.Max(0.01f, transform.localScale.y), 0f);
            root.transform.localScale = Vector3.one * (config.barScale / Mathf.Max(0.01f, transform.localScale.x));
            System.Func<string, Sprite, int, SpriteRenderer> mk = delegate(string nm, Sprite sp, int ord)
            {
                var g = new GameObject(nm);
                g.transform.SetParent(root.transform, false);
                var r = g.AddComponent<SpriteRenderer>();
                r.sprite = sp; r.sortingOrder = ord; r.sharedMaterial = FxUnlit.Mat;
                return r;
            };
            mk("under", config.barUnder, 800);
            var fill = mk("fill", config.barProgress, 801);
            mk("over", config.barOver, 802);
            barFill = fill.transform;
            barFullW = config.barProgress.bounds.size.x;
            UpdateBar();
        }

        private void UpdateBar()
        {
            if (barFill == null) return;
            float r = Mathf.Clamp01((float)hp / config.maxHp);
            barFill.localScale = new Vector3(r, 1f, 1f);
            barFill.localPosition = new Vector3(-(1f - r) * barFullW * 0.5f, 0f, 0f);
        }

        private void SetState(int s)
        {
            state = s; animT = 0f; stateT = 0f; dealtThisSwing = false; holdDone = false; holdT = 0f;
            swingResolved[0] = false; swingResolved[1] = false;
            cur = s == 0 ? idleF : s == 1 ? walkF : s == 2 ? (atkIs1 ? atk1F : atk2F) : s == 3 ? hitF : s == 5 ? hitF : deathF;
            curFps = s == 0 ? config.fpsIdle : s == 1 ? config.fpsWalk : s == 2 ? config.fpsAtk : config.fpsHit;
            if (s == 4) curFps = config.fpsDeath;
            if (s == 5) BeginGroggyFx(); else EndGroggyFx();
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
            if (state == 4) return;
            hp -= 1; // 타격 1회 = 10% 고정 (요청 명세)
            UpdateBar();
            if (hp <= 0) { SetState(4); return; }
            if (state != 5) SetState(3); // 그로기 중엔 그로기 유지, 그 외엔 항상 피격 모션
        }

        private void Update()
        {
            if (config == null || cur == null || cur.Length == 0) return;
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) lastParryPress = Time.time;
            bool holding = state == 2 && atkIs1 && !holdDone && (int)animT >= config.atk1HoldFrame;
            if (holding)
            {
                holdT += Time.deltaTime;
                if (holdT >= config.atk1HoldTime) { holdDone = true; holding = false; }
            }
            if (!holding)
            {
                animT += Time.deltaTime * curFps;
                stateT += Time.deltaTime;
            }
            bool loop = state == 0 || state == 1;
            int idx = loop ? (int)animT % cur.Length : Mathf.Min((int)animT, cur.Length - 1);
            sr.sprite = cur[idx];
            if (groggyFx != null) groggyFx.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 6f) * 14f);
            if (player != null && state != 4 && state != 2 && state != 5) sr.flipX = player.position.x > transform.position.x;

            if (state == 4) { if ((int)animT >= cur.Length - 1) enabled = false; return; }
            if (player == null) return;
            float dx = Mathf.Abs(player.position.x - transform.position.x);

            if (state == 0)
            {
                if (dx <= config.aggroX && dx > config.attackRange) SetState(1);
                else if (dx <= config.attackRange && Time.time >= nextAtk) BeginAttack();
            }
            else if (state == 1)
            {
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                transform.position += new Vector3(dir * config.walkSpeed * Time.deltaTime, 0f, 0f);
                if (dx <= config.attackRange && Time.time >= nextAtk) BeginAttack();
                else if (dx > config.aggroX) SetState(0);
            }
            else if (state == 2 && atkIs1)
            {
                // 이단 베기: 프레임 창(5~8, 11~14) 안에서 C를 누르면 즉시 패링 성공
                for (int w = 0; w < 2; w++)
                {
                    int ws = w == 0 ? config.atk1Win1Start : config.atk1Win2Start;
                    int we = w == 0 ? config.atk1Win1End : config.atk1Win2End;
                    if (swingResolved[w]) continue;
                    bool inWin = idx >= ws && idx <= we;
                    if (inWin && ParryBuffered())
                    {
                        swingResolved[w] = true;
                        if (config.clashConfig != null)
                            ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, config.clashConfig);
                        player.SendMessage("AddMp", config.damage * 10, SendMessageOptions.DontRequireReceiver);
                        if (config.showParryDebug) DebugPopup("패링 OK", new Color(0.3f, 1f, 0.4f));
                        parryCount++;
                        if (parryCount >= config.groggyNeed) { parryCount = 0; SetState(5); return; }
                    }
                    else if (!inWin && idx > we)
                    {
                        swingResolved[w] = true; // 창 종료 — 미패링이면 피해
                        if (dx <= config.hitReach)
                        {
                            player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                            if (config.showParryDebug)
                            {
                                float since = Time.time - lastParryPress;
                                DebugPopup(since > 3f ? "패링 입력 없음" : "창 밖 " + since.ToString("F2") + "초 전 입력", new Color(1f, 0.35f, 0.3f));
                            }
                        }
                    }
                }
                float frac1 = stateT / config.attackDuration;
                if (frac1 >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
            }
            else if (state == 2)
            {
                float frac = stateT / config.attackDuration;
                float wS = config.hit2FracStart;
                float wE = config.hit2FracEnd;
                if (!dealtThisSwing && frac >= wS && frac <= wE && dx <= config.hitReach)
                {
                    dealtThisSwing = true;
                    bool parried = false;
                    // atk2 버퍼 선점: 창 진입 시 최근 0.2초 입력이 있으면 성공 (일찍 눌러도 OK)
                    if (ParryBuffered()) parried = true;
                    if (!parried && controller != null && tryParry != null)
                    {
                        object r = tryParry.Invoke(controller, new object[] { gameObject });
                        parried = r is bool && (bool)r;
                    }
                    if (parried)
                    {
                        if (config.clashConfig != null)
                            ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, config.clashConfig);
                        player.SendMessage("AddMp", config.damage * 10, SendMessageOptions.DontRequireReceiver);
                        if (config.showParryDebug) DebugPopup("패링 OK", new Color(0.3f, 1f, 0.4f));
                        parryCount++;
                        if (parryCount >= config.groggyNeed) { parryCount = 0; SetState(5); return; }
                    }
                    else
                    {
                        player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                        if (config.showParryDebug)
                        {
                            float since = Time.time - lastParryPress;
                            DebugPopup(since > 3f ? "패링 입력 없음" : "너무 빨랐다 " + since.ToString("F2") + "초 일찍", new Color(1f, 0.35f, 0.3f));
                        }
                    }
                }
                if (frac >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
            }
            else if (state == 3)
            {
                if ((int)animT >= cur.Length) SetState(0);
            }
            else if (state == 5)
            {
                if (stateT >= config.groggyTime) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
            }
        }

        private void DebugPopup(string msg, Color col)
        {
            var go = new GameObject("ParryDebug");
            go.transform.position = player.position + Vector3.up * 2.2f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = msg; tm.fontSize = 44; tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter; tm.color = col;
            go.GetComponent<MeshRenderer>().sortingOrder = 950;
            go.AddComponent<PopupFloater>().Init(1.2f, 1.1f);
        }

        private void BeginAttack()
        {
            atkIs1 = Random.value < 0.5f;
            if (player != null) sr.flipX = player.position.x > transform.position.x;
            SetState(2);
        }
    }
}
