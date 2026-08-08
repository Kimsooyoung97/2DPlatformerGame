using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    // AdventureScene4 데몬 보스. 입장 시 transform(슬라임→데몬) 인트로 후 전투.
    // 상태: -1 transform / 0 idle / 1 walk / 2 cleave / 7 smash / 3 cast / 6 hit / 5 groggy / 4 death
    public class DemonBoss : MonoBehaviour
    {
        public DemonBossConfig config;
        public Sprite[] introFrames, idleFrames, walkFrames, cleaveFrames, smashFrames, castFrames, hitFrames, deathFrames;
        public Sprite[] projFly, projBoom;

        private int state = -1;
        private float stateT, animT, nextAtk;
        private int hp, parryCount;
        private bool dealtThisSwing, castFired;
        private SpriteRenderer sr;
        private Transform player;
        private Component controller;
        private System.Reflection.MethodInfo tryParry;
        private float lastParryPress = -999f, lastConsumed = -999f;
        private TextMesh groggyPips;
        private GameObject groggyFx, burstMsg;
        private Coroutine flashCo, sparkleCo, dashCo;
        private SpriteRenderer playerSr;

        void Start()
        {
            if (config == null) { Debug.LogError("[DemonBoss] config 미배선! 인스펙터에서 DemonBossConfig 연결 필요"); enabled = false; return; }
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            hp = config.maxHp;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.useFullKinematicContacts = true; // FAIL: Kinematic 트리거
            var pgo = GameObject.Find("Player");
            if (pgo != null)
            {
                player = pgo.transform;
                playerSr = pgo.GetComponent<SpriteRenderer>();
                foreach (var mb in pgo.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry");
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }
            BuildGroggyPips();
            SetState(-1); // 변신 인트로
        }

        private bool ParryBuffered()
        {
            if (Time.time - lastParryPress <= config.parryBuffer && lastParryPress > lastConsumed)
            { lastConsumed = lastParryPress; return true; }
            return false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) lastParryPress = Time.time;
            stateT += Time.deltaTime; animT += Time.deltaTime * config.fps;
            SnapToGround();

            if (state == 4) { Anim(deathFrames, false); return; }
            if (state == -1)
            {
                Anim(introFrames, false);
                if ((int)animT >= introFrames.Length) { nextAtk = Time.time + 1.0f; SetState(0); }
                return;
            }
            if (player == null) return;
            float dx = Mathf.Abs(player.position.x - transform.position.x);
            float side = player.position.x < transform.position.x ? -1f : 1f; // 플레이어가 있는 월드 방향(이동용)
            if (state != 5) sr.flipX = BossFacingLogic.ShouldFlipX(transform.position.x, player.position.x, config.spriteFacesLeft);

            if (state == 5)
            {
                if (burstMsg != null) burstMsg.transform.position = player.position + Vector3.up * 2.6f;
                if (kb != null && kb.zKey.wasPressedThisFrame && dashCo == null && dx > 3.4f)
                    dashCo = StartCoroutine(DashToBoss());
                Anim(hitFrames, true);
                if (stateT >= config.groggyTime) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
                return;
            }
            if (state == 6)
            {
                Anim(hitFrames, false);
                if ((int)animT >= hitFrames.Length) SetState(0);
                return;
            }
            if (state == 2) { DoCleave(dx); return; }
            if (state == 7) { DoSmash(dx, side); return; }
            if (state == 3) { DoCast(); return; }

            // idle/walk 판단
            if (dx > config.aggroX) { Anim(idleFrames, true); return; }
            if (Time.time >= nextAtk && dx <= config.cleaveReach) { SetState(2); return; }
            if (Time.time >= nextAtk && dx <= config.aggroX)
            {
                float roll = Random.value;
                if (roll < config.castChance) { SetState(3); return; }
                if (roll < config.castChance + config.smashChance) { SetState(7); return; }
            }
            // 접근
            transform.position += new Vector3(side * config.walkSpeed * Time.deltaTime, 0f, 0f);
            Anim(walkFrames, true);
        }

        private void DoCleave(float dx)
        {
            Anim(cleaveFrames, false);
            float frac = stateT / config.cleaveDur;
            if (!dealtThisSwing && BossRangeLogic.WindowOpen(frac, config.cleaveWinS, config.cleaveWinE) && InHitBand(config.cleaveReach))
                ResolveHit();
            if (frac >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
        }

        private void DoSmash(float dx, float side)
        {
            Anim(smashFrames, false);
            float frac = stateT / config.smashDur;
            if (frac < config.smashWinS && dx > config.smashStopX)
                transform.position += new Vector3(side * config.smashApproachSpeed * Time.deltaTime, 0f, 0f);
            if (!dealtThisSwing && BossRangeLogic.WindowOpen(frac, config.smashWinS, config.smashWinE) && InHitBand(config.smashReach))
                ResolveHit();
            if (frac >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
        }

        private void DoCast()
        {
            Anim(castFrames, false);
            float frac = stateT / config.castDur;
            if (!castFired && frac >= config.castFireFrac)
            {
                castFired = true;
                var go = new GameObject("DemonProj");
                float face = Facing(); // 실제로 바라보는 월드 방향
                go.transform.position = new Vector3(
                    BossFacingLogic.HandWorldX(transform.position.x, config.handOffset.x, face),
                    BossFacingLogic.HandWorldY(transform.position.y, config.handOffset.y), 0f);
                go.transform.localScale = Vector3.one; // PPU 33.3 = 3배 크기
                var proj = go.AddComponent<DemonProjectile>();
                Vector2 dir = player != null ? ((Vector2)(player.position + Vector3.up * 0.8f - go.transform.position)).normalized : new Vector2(face, 0f);
                proj.Launch(projFly, projBoom, dir, config.projSpeed, config.fps, config.projDamage, config.projLife, config.clashConfig);
            }
            if (frac >= 1f) { nextAtk = Time.time + config.attackCooldown; SetState(0); }
        }

        // 현재 바라보는 월드 방향 (+1 오른쪽 / -1 왼쪽)
        private float Facing()
        {
            return BossFacingLogic.FacingSign(sr.flipX, config.spriteFacesLeft);
        }

        // 등 뒤 타격 방지
        private bool TargetInFront()
        {
            if (player == null) return false;
            return BossFacingLogic.TargetInFront(transform.position.x, player.position.x, Facing(), config.frontDeadZone);
        }

        // 실제 타격 판정. 디버그 표시도 같은 함수를 쓴다(표시와 판정 불일치 방지)
        private bool InHitBand(float reach)
        {
            if (player == null) return false;
            return BossRangeLogic.InHitBand(transform.position.x, player.position.x, reach, Facing(), config.frontDeadZone);
        }

        // 발끝을 아레나 지면에 고정 (공중 부양 방지)
        private void SnapToGround()
        {
            float y = BossFacingLogic.GroundedPivotY(config.groundY, config.feetOffset);
            var p = transform.position;
            if (!Mathf.Approximately(p.y, y)) transform.position = new Vector3(p.x, y, p.z);
        }

        private void ResolveHit()
        {
            dealtThisSwing = true;
            bool parried = ParryBuffered();
            if (!parried && controller != null && tryParry != null)
            {
                object r = tryParry.Invoke(controller, new object[] { gameObject });
                parried = r is bool && (bool)r;
            }
            if (parried)
            {
                if (config.clashConfig != null && player != null)
                    ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 2f, config.clashConfig);
                PlayerMana.RewardParry(player);
                parryCount++;
                RefreshGroggyPips();
                if (parryCount >= config.groggyNeed) { parryCount = 0; RefreshGroggyPips(); SetState(5); }
            }
            else if (player != null)
                player.SendMessage("TakeDamage", (float)config.damage, SendMessageOptions.DontRequireReceiver);
        }

        public void TakeDamage(int dmg)
        {
            if (state == 4 || state == -1) return;
            hp -= 1;
            HitFeedback();
            if (hp <= 0) { SetState(4); return; }
            if (state != 5) SetState(6);
        }

        private void HitFeedback()
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRed());
            var pop = new GameObject("DemonHpPopup");
            pop.transform.position = transform.position + Vector3.up * (config.groggyFxOffsetY + 1.2f);
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
            yield return new WaitForSeconds(config.hitFlash);
            sr.color = Color.white;
            flashCo = null;
        }

        // ===== 공격 범위 디버그 표시 (config.showRangesInGame) =====
        // 판정과 표시가 어긋나지 않도록 BossRangeLogic 의 같은 함수로 좌표를 만든다.
        private LineRenderer[] bands;
        private TextMesh rangeLabel;

        private LineRenderer MakeBand(string name, Color c, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;   // 부모 스케일·반전 영향 차단
            lr.loop = true; lr.positionCount = 4;
            lr.startWidth = width; lr.endWidth = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = c; lr.endColor = c;
            lr.sortingOrder = 860;
            return lr;
        }

        private void BuildRangeGizmos()
        {
            bands = new LineRenderer[4];
            bands[0] = MakeBand("Band_Aggro", new Color(1f, 0.9f, 0.2f, 0.35f), 0.08f);   // 노랑: 인지
            bands[1] = MakeBand("Band_Smash", new Color(1f, 0.3f, 1f, 0.55f), 0.10f);     // 자홍: 스매시 리치
            bands[2] = MakeBand("Band_Cleave", new Color(1f, 0.25f, 0.25f, 0.7f), 0.12f); // 빨강: 클리브 리치
            bands[3] = MakeBand("Band_SmashStop", new Color(0.35f, 0.7f, 1f, 0.7f), 0.08f); // 파랑: 스매시 정지선

            var go = new GameObject("RangeLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, config.groggyFxOffsetY + 2.6f, 0f);
            rangeLabel = go.AddComponent<TextMesh>();
            rangeLabel.fontSize = 40; rangeLabel.characterSize = 0.055f;
            rangeLabel.anchor = TextAnchor.MiddleCenter;
            rangeLabel.color = new Color(0.85f, 1f, 0.85f);
            go.GetComponent<MeshRenderer>().sortingOrder = 903;
        }

        private void DestroyRangeGizmos()
        {
            if (bands != null) { foreach (var lr in bands) if (lr != null) Destroy(lr.gameObject); bands = null; }
            if (rangeLabel != null) { Destroy(rangeLabel.gameObject); rangeLabel = null; }
        }

        private void SetRect(LineRenderer lr, float xMin, float xMax, float yMin, float yMax)
        {
            lr.SetPosition(0, new Vector3(xMin, yMin, 0f));
            lr.SetPosition(1, new Vector3(xMax, yMin, 0f));
            lr.SetPosition(2, new Vector3(xMax, yMax, 0f));
            lr.SetPosition(3, new Vector3(xMin, yMax, 0f));
        }

        void LateUpdate()
        {
            if (config == null) return;
            if (!config.showRangesInGame) { if (bands != null || rangeLabel != null) DestroyRangeGizmos(); return; }
            if (bands == null) BuildRangeGizmos();

            float bx = transform.position.x;
            float face = Facing();
            float dz = config.frontDeadZone;
            float y0 = config.groundY;
            float y1 = config.groundY + config.rangeBandHeight;

            // 인지 범위는 좌우 양쪽 (판정이 Mathf.Abs 기준)
            SetRect(bands[0], bx - config.aggroX, bx + config.aggroX, y0, y1);
            SetRect(bands[1], BossRangeLogic.BandMinX(bx, config.smashReach, face, dz),
                              BossRangeLogic.BandMaxX(bx, config.smashReach, face, dz), y0, y1 * 0.85f);
            SetRect(bands[2], BossRangeLogic.BandMinX(bx, config.cleaveReach, face, dz),
                              BossRangeLogic.BandMaxX(bx, config.cleaveReach, face, dz), y0, y1 * 0.7f);
            float stopX = bx + config.smashStopX * face;
            SetRect(bands[3], stopX - 0.05f, stopX + 0.05f, y0, y1 * 0.55f);

            // 타격 시간창이 열린 동안 해당 띠를 굵고 밝게
            bool cleaveOpen = state == 2 && BossRangeLogic.WindowOpen(stateT / config.cleaveDur, config.cleaveWinS, config.cleaveWinE);
            bool smashOpen = state == 7 && BossRangeLogic.WindowOpen(stateT / config.smashDur, config.smashWinS, config.smashWinE);
            Highlight(bands[2], cleaveOpen, new Color(1f, 0.25f, 0.25f, 0.7f));
            Highlight(bands[1], smashOpen, new Color(1f, 0.3f, 1f, 0.55f));

            if (rangeLabel != null)
            {
                if (!config.showRangeLabels) { rangeLabel.text = string.Empty; return; }
                float dx = player != null ? Mathf.Abs(player.position.x - bx) : -1f;
                string cur = "-";
                if (state == 2) cur = string.Format("CLEAVE {0:F2} 창 {1:F2}~{2:F2}", stateT / config.cleaveDur, config.cleaveWinS, config.cleaveWinE);
                else if (state == 7) cur = string.Format("SMASH {0:F2} 창 {1:F2}~{2:F2}", stateT / config.smashDur, config.smashWinS, config.smashWinE);
                else if (state == 3) cur = string.Format("CAST {0:F2} 발사 {1:F2}", stateT / config.castDur, config.castFireFrac);
                rangeLabel.text = string.Format("dx {0:F1} | 바라봄 {1} | cleave {2:F1}{3} | smash {4:F1}{5}\n{6}{7}",
                    dx, face < 0f ? "◀" : "▶",
                    config.cleaveReach, InHitBand(config.cleaveReach) ? "✔" : "✘",
                    config.smashReach, InHitBand(config.smashReach) ? "✔" : "✘",
                    cur, (cleaveOpen || smashOpen) ? "  ◆타격중" : "");
            }
        }

        private void Highlight(LineRenderer lr, bool on, Color baseCol)
        {
            if (lr == null) return;
            float w = on ? 0.30f : 0.12f;
            lr.startWidth = w; lr.endWidth = w;
            var c = on ? new Color(1f, 1f, 0.5f, 0.95f) : baseCol;
            lr.startColor = c; lr.endColor = c;
        }

        // 씬 뷰: 노랑=인지 / 빨강=클리브 / 자홍=스매시 (수평 판정이므로 선으로 표시)
        void OnDrawGizmosSelected()
        {
            if (config == null) return;
            float bx = transform.position.x;
            float y0 = config.groundY, y1 = config.groundY + config.rangeBandHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(bx, (y0 + y1) * 0.5f, 0f), new Vector3(config.aggroX * 2f, y1 - y0, 0f));
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(new Vector3(bx, (y0 + y1) * 0.5f, 0f), new Vector3(config.smashReach * 2f, y1 - y0, 0f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(bx, (y0 + y1) * 0.5f, 0f), new Vector3(config.cleaveReach * 2f, y1 - y0, 0f));
        }

        private void SetState(int s)
        {
            state = s; stateT = 0f; animT = 0f; dealtThisSwing = false; castFired = false;
            if (s == 5) { BeginGroggyFx(); BeginBurst(); } else { EndGroggyFx(); EndBurst(); }
        }

        private void Anim(Sprite[] arr, bool loop)
        {
            if (arr == null || arr.Length == 0) return;
            int i = (int)animT;
            sr.sprite = arr[loop ? i % arr.Length : Mathf.Min(i, arr.Length - 1)];
        }

        private void BuildGroggyPips()
        {
            var go = new GameObject("GroggyPips");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, config.groggyFxOffsetY, 0f);
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

        private void BeginGroggyFx()
        {
            groggyFx = new GameObject("GroggyStars");
            groggyFx.transform.SetParent(transform, false);
            groggyFx.transform.localPosition = new Vector3(0f, config.groggyFxOffsetY - 0.9f, 0f);
            var tm = groggyFx.AddComponent<TextMesh>();
            tm.text = "\u2605\u2605\u2605";
            tm.fontSize = 44; tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.9f, 0.25f);
            groggyFx.GetComponent<MeshRenderer>().sortingOrder = 901;
        }

        private void EndGroggyFx() { if (groggyFx != null) Destroy(groggyFx); }

        private void BeginBurst()
        {
            PlayerController2D.AttackSpeedMul = 2f;
            burstMsg = new GameObject("BurstMsg");
            burstMsg.transform.position = (player != null ? player.position : transform.position) + Vector3.up * 2.6f;
            var tm = burstMsg.AddComponent<TextMesh>();
            tm.text = "Z \uc5f0\ud0c0! \uacf5\uaca9 \ucc2c\uc2a4!";
            tm.fontSize = 52; tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.85f, 0.2f);
            burstMsg.GetComponent<MeshRenderer>().sortingOrder = 950;
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
                yield return new WaitForSeconds(0.22f);
            }
        }

        private System.Collections.IEnumerator DashToBoss()
        {
            PlayerController2D.InputLocked = true;
            var rb2 = player != null ? player.GetComponent<Rigidbody2D>() : null;
            float side2 = player.position.x < transform.position.x ? -1f : 1f;
            Vector3 target = transform.position + new Vector3(side2 * 3.2f, 0f, 0f);
            target.y = player.position.y;
            while (state == 5 && Vector2.Distance(player.position, target) > 0.08f)
            {
                player.position = Vector3.MoveTowards(player.position, target, 20f * Time.deltaTime);
                if (rb2 != null) rb2.linearVelocity = Vector2.zero;
                yield return null;
            }
            PlayerController2D.InputLocked = false;
            dashCo = null;
        }
    }
}
