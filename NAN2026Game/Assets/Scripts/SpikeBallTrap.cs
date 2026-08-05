using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    // 천장 스파이크볼: 대기 → (시야x2) 점멸 경고 → (시야x1.1) 조준 돌진 → 패링 판정
    public class SpikeBallTrap : MonoBehaviour
    {
        public SpikeBallConfig config;
        public Transform player;
        SpriteRenderer sr;
        System.Reflection.MethodInfo tryParry;
        Component controller;
        Vector3 home;
        Vector2 dir;
        int phase; // 0대기 1경고 2돌진 3사멸
        float respawnAt;
        float visionR;

        void Start()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            home = transform.position;
            if (player == null) { var p = GameObject.Find("Player"); if (p != null) player = p.transform; }
            visionR = config != null ? config.visionRadiusFallback : 4.5f;
            if (player != null)
            {
                foreach (var c in player.GetComponents<Component>())
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "PlayerController2D")
                    { controller = c; tryParry = c.GetType().GetMethod("TryParry"); }
                }
                var pv = player.Find("PlayerVisionLight");
                if (pv != null)
                {
                    var l2 = pv.GetComponent("UnityEngine.Rendering.Universal.Light2D");
                    if (l2 != null)
                    {
                        var pr = l2.GetType().GetProperty("pointLightOuterRadius");
                        if (pr != null) visionR = (float)pr.GetValue(l2, null);
                    }
                }
            }
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.isTrigger = true;
        }

        void Update()
        {
            if (config == null || player == null) return;
            if (phase == 3)
            {
                if (Time.time >= respawnAt)
                { transform.position = home; phase = 0; SetAlpha(1f); if (sr != null) sr.enabled = true; }
                return;
            }
            if (phase == 2)
            {
                transform.position += (Vector3)(dir * config.launchSpeed * Time.deltaTime);
                transform.Rotate(0f, 0f, config.spinDegPerSec * Time.deltaTime);
                if (transform.position.y < 2.6f || Vector3.Distance(transform.position, home) > 40f) Break(false);
                return;
            }
            float dist = Mathf.Abs(transform.position.x - player.position.x); // 천장 트랩: 수평거리 기준
            int p = SpikeBallLogic.Phase(dist, visionR, config.warnMultiplier, config.launchMultiplier);
            if (p >= 2 && phase != 2)
            {
                float dx, dy;
                SpikeBallLogic.LaunchDir(transform.position.x, transform.position.y, player.position.x, player.position.y + 0.4f, out dx, out dy);
                dir = new Vector2(dx, dy);
                phase = 2; SetAlpha(1f);
                return;
            }
            phase = p;
            SetAlpha(phase == 1 ? SpikeBallLogic.BlinkAlpha(Time.time, config.blinkHz) : 1f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (phase != 2) return;
            if (other.transform.root != null && player != null && other.transform.root == player.root)
            {
                bool ok = false;
                if (controller != null && tryParry != null)
                {
                    object r = tryParry.Invoke(controller, new object[] { gameObject });
                    ok = r is bool && (bool)r;
                }
                Popup(ok ? "패링 성공!" : "패링 실패!", ok ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.3f, 0.25f));
                if (ok) { dir = new Vector2(-dir.x, Mathf.Abs(dir.y)); Invoke("BreakSilent", 0.5f); }
                else { player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver); Break(false); }
            }
        }

        void BreakSilent() { Break(false); }

        void Break(bool silent)
        {
            phase = 3;
            respawnAt = Time.time + (config != null ? config.respawnDelay : 3f);
            if (sr != null) sr.enabled = false;
        }

        void SetAlpha(float a)
        {
            if (sr == null) return;
            var c = sr.color; c.a = a; sr.color = c;
        }

        void Popup(string msg, Color col)
        {
            var go = new GameObject("ParryJudgePopup");
            go.transform.position = player.position + Vector3.up * 1.4f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = msg;
            tm.fontSize = config.popupFontSize;
            tm.characterSize = config.popupCharSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = col;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = 900;
            go.AddComponent<PopupFloater>().Init(config.popupRise, config.popupLife);
        }
    }

    public class PopupFloater : MonoBehaviour
    {
        float rise, life, t;
        TextMesh tm;
        public void Init(float r, float l) { rise = r; life = l; tm = GetComponent<TextMesh>(); }
        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (rise / life) * Time.deltaTime;
            if (tm != null) { var c = tm.color; c.a = Mathf.Clamp01(1f - t / life); tm.color = c; }
            if (t >= life) Destroy(gameObject);
        }
    }
}
