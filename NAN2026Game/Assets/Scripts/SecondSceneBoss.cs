using UnityEngine;
using UnityEngine.UI;
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
        private Image barFillImg;
        private GameObject hudGo;
        private GameObject groggyFx;
        private TextMesh groggyPips;
        private GameObject burstMsg;
        private Coroutine sparkleCo, dashCo;
        private SpriteRenderer playerSr;
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
            BuildGroggyPips();
            SetState(0);
        }

        private void BuildBar()
        {
            if (config.barUnder == null) return;
            var cgo = new GameObject("BossHpHud");
            hudGo = cgo;
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            System.Func<string, Sprite, int, Image> mk = delegate(string nm, Sprite sp, int ord)
            {
                var g = new GameObject(nm);
                g.transform.SetParent(cgo.transform, false);
                var img = g.AddComponent<Image>();
                img.sprite = sp; img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(config.barScale * 260f, config.barScale * 104f);
                rt.anchoredPosition = new Vector2(0f, -config.barOffsetY * 22f);
                return img;
            };
            mk("under", config.barUnder, 0);
            barFillImg = mk("fill", config.barProgress, 1);
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin = 0;
            mk("over", config.barOver, 2);
            UpdateBar();
            cgo.SetActive(false); // 보스전 개시 전엔 숨김
        }

        private void UpdateBar()
        {
            if (barFillImg == null) return;
            barFillImg.fillAmount = Mathf.Clamp01((float)hp / config.maxHp);
        }

        private void SetState(int s)
        {
            state = s; animT = 0f; stateT = 0f; dealtThisSwing = false; holdDone = false; holdT = 0f;
            swingResolved[0] = false; swingResolved[1] = false;
            cur = s == 0 ? idleF : s == 1 ? walkF : s == 2 ? (atkIs1 ? atk1F : atk2F) : s == 3 ? hitF : s == 5 ? hitF : deathF;
            curFps = s == 0 ? config.fpsIdle : s == 1 ? config.fpsWalk : s == 2 ? config.fpsAtk : config.fpsHit;
            if (s == 4) curFps = config.fpsDeath;
            if (s == 5) { BeginGroggyFx(); BeginBurst(); } else { EndGroggyFx(); EndBurst(); }
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
            // 안내 문구 (그로기 동안 유지)
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
            while (state == 5)
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
            // Z 자동 대시: 컨트롤러 잠깐 끄고 보스 앞까지 고속 이동
            PlayerController2D.InputLocked = true; // 입력 게이트
            var rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
            float side = player.position.x < transform.position.x ? -1f : 1f;
            Vector3 target = transform.position + new Vector3(side * config.burstDashStopX, 0f, 0f);
            target.y = player.position.y;
            while (state == 5 && Vector2.Distance(player.position, target) > 0.08f)
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
            if (kb != null && kb.spaceKey.wasPressedThisFrame) lastParryPress = Time.time;
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
            if (hudGo != null && !hudGo.activeSelf && dx <= config.aggroX) hudGo.SetActive(true); // 접근 시 체력바 등장

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
                        PlayerMana.RewardParry(player);
                        if (config.showParryDebug) DebugPopup("패링 OK", new Color(0.3f, 1f, 0.4f));
                        parryCount++;
                        RefreshGroggyPips();
                        if (parryCount >= config.groggyNeed) { parryCount = 0; RefreshGroggyPips(); SetState(5); return; }
                    }
                    else if (!inWin && idx > we)
                    {
                        swingResolved[w] = true; // 창 종료 — 미패링이면 피해
                        if (dx <= config.hitReach)
                        {
                            player.SendMessage("TakeDamage", (float)config.damage, SendMessageOptions.DontRequireReceiver);
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
                        PlayerMana.RewardParry(player);
                        if (config.showParryDebug) DebugPopup("패링 OK", new Color(0.3f, 1f, 0.4f));
                        parryCount++;
                        RefreshGroggyPips();
                        if (parryCount >= config.groggyNeed) { parryCount = 0; RefreshGroggyPips(); SetState(5); return; }
                    }
                    else
                    {
                        player.SendMessage("TakeDamage", (float)config.damage, SendMessageOptions.DontRequireReceiver);
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
                if (burstMsg != null && player != null)
                    burstMsg.transform.position = player.position + Vector3.up * 2.6f;
                if (kb != null && kb.zKey.wasPressedThisFrame && dashCo == null && dx > config.burstDashStopX + 0.5f)
                    dashCo = StartCoroutine(DashToBoss());
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
