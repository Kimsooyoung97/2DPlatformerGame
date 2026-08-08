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
                    { controller = c; windowActive = controller != null ? controller.GetType().GetMethod("IsParryWindowActive") : null;
                var mcT = System.Type.GetType("NAN2026.MovementConfig, Assembly-CSharp");
                var mcF = controller != null ? controller.GetType().GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
                var mcV = mcF != null ? mcF.GetValue(controller) : null;
                var prF = mcV != null ? mcV.GetType().GetField("parryReachX") : null;
                if (prF != null) parryReach = (float)prF.GetValue(mcV);
                tryParry = c.GetType().GetMethod("TryParry"); }
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
                { transform.position = home; phase = 0; resolved = false; SetAlpha(1f); if (sr != null) sr.enabled = true; }
                return;
            }
            if (phase == 2)
            {
                transform.position += (Vector3)(dir * config.launchSpeed * Time.deltaTime);
                // 조기 패링: 이펙트 리치(parryReachX) 안이고 창 활성이면 접촉 전 성공 처리
                if (!resolved && controller != null && parryReach > 0f)
                {
                    float ddx = transform.position.x - player.position.x;
                    float pface = 0f;
                    var psr = player.GetComponentInChildren<UnityEngine.SpriteRenderer>();
                    if (psr != null) pface = psr.flipX ? -1f : 1f;
                    bool inFront = pface != 0f && ddx * pface > 0f;
                    float d2 = UnityEngine.Vector2.Distance(transform.position, player.position);
                    if (inFront && d2 <= parryReach && windowActive != null)
                    {
                        object w = windowActive.Invoke(controller, null);
                        if (w is bool && (bool)w) ResolveHit();
                    }
                }
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
                ResolveHit();
        }

        bool resolved;
        float parryReach = 1.5f;
        System.Reflection.MethodInfo windowActive;
        void ResolveHit()
        {
            if (resolved) return;
            resolved = true;

            bool ok = false;
            if (controller != null && tryParry != null)
            {
                object result = tryParry.Invoke(controller, new object[] { gameObject });
                ok = result is bool && (bool)result;
            }

            if (ok)
            {
                ParryClashFx.Play(
                    (transform.position + player.position) * 0.5f + Vector3.up * 0.8f,
                    config);
                Popup("패링 성공!", new Color(0.35f, 1f, 0.45f));
                PlayerMana.RewardParry(player);
                SpikeParryEvents.Report();
                dir = new Vector2(-dir.x, Mathf.Abs(dir.y));
                Invoke("BreakSilent", 0.5f);
                return;
            }

            Popup("패링 실패!", new Color(1f, 0.3f, 0.25f));
            if (player != null)
                player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
            Break(false);
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

        public static void ShowAt(Vector3 pos, string msg, Color col, SpikeBallConfig cfg)
        {
            var go = new GameObject("ParryJudgePopup");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = msg;
            tm.fontSize = cfg != null ? cfg.popupFontSize : 48;
            tm.characterSize = cfg != null ? cfg.popupCharSize : 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = col;
            go.GetComponent<MeshRenderer>().sortingOrder = 900;
            go.AddComponent<PopupFloater>().Init(cfg != null ? cfg.popupRise : 1.2f, cfg != null ? cfg.popupLife : 0.7f);
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

    public static class ClashSfx
    {
        public static void PlaySegment(AudioClip clip, float vol, float startMs, float endMs)
        {
            var go = new GameObject("ClashSfx");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.volume = vol; src.playOnAwake = false;
            float st = Mathf.Clamp(startMs / 1000f, 0f, clip.length);
            float en = endMs <= 0f ? clip.length : Mathf.Clamp(endMs / 1000f, st, clip.length);
            src.time = st; src.Play();
            var stopper = go.AddComponent<ClashSfxStopper>();
            stopper.stopAt = en - st;
        }
    }
    public class ClashSfxStopper : MonoBehaviour
    {
        public float stopAt; float t;
        void Update() { t += Time.unscaledDeltaTime; if (t >= stopAt) Destroy(gameObject); }
    }

    public static class ParryClashFx
    {
        public static void Play(Vector3 pos, SpikeBallConfig cfg)
        {
            var go = new GameObject("ParryClash");
            go.transform.position = pos;
            var f = go.AddComponent<ClashFlash>();
            f.Init(cfg != null ? cfg.clashDuration : 0.16f, cfg != null ? cfg.clashLines : 8, cfg != null ? cfg.clashRadius : 1.3f, cfg != null ? cfg.clashHitstop : 0.08f, cfg);
            if (cfg != null && cfg.clashSound != null)
                ClashSfx.PlaySegment(cfg.clashSound, cfg.clashVolume, cfg.clashSoundStartMs, cfg.clashSoundEndMs);
        }
    }

    public class ClashFlash : MonoBehaviour
    {
        float dur, radius, t; int lines; float restoreAt = -1f;
        Transform recoilCam; Vector3 camBase; float recoilT; bool recoilOn = false;
        LineRenderer[] rays; SpriteRenderer flash;
        static UnityEngine.Sprite dot;
        SpikeBallConfig cfgRef;
        public void Init(float d, int n, float r, float hitstop, SpikeBallConfig cfg2 = null)
        {
            if (hitstop > 0f && d < hitstop + 0.05f) d = hitstop + 0.05f; // 수명 < 히트스톱이면 timeScale 영구 0 — 보정
            dur = d; lines = n; radius = r; cfgRef = cfg2;
            if (dot == null)
            {
                var tx2 = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int i = 0; i < 16; i++) tx2.SetPixel(i % 4, i / 4, Color.white);
                tx2.Apply();
                dot = Sprite.Create(tx2, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            flash = gameObject.AddComponent<SpriteRenderer>(); flash.sharedMaterial = NAN2026.FxUnlit.Mat;
            flash.sprite = dot; flash.color = Color.white; flash.sortingOrder = 950;
            rays = new LineRenderer[lines];
            for (int i = 0; i < lines; i++)
            {
                var lg = new GameObject("ray"); lg.transform.SetParent(transform, false);
                var lr = lg.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startWidth = 0.06f; lr.endWidth = 0.0f;
                lr.positionCount = 2; lr.sortingOrder = 949;
                lr.startColor = new Color(0.85f, 0.95f, 1f, 1f); lr.endColor = new Color(0.85f, 0.95f, 1f, 0f);
                rays[i] = lr;
            }
            if (hitstop > 0f) { Time.timeScale = 0f; restoreAt = Time.unscaledTime + hitstop; }
        }
        void OnDestroy() { if (restoreAt > 0f) Time.timeScale = 1f; } // 안전핀: 복구 전 소멸해도 시간 복원
        void Update()
        {
            if (restoreAt > 0f && Time.unscaledTime >= restoreAt)
            {
                Time.timeScale = 1f; restoreAt = -1f;
                if (cfgRef != null && cfgRef.clashRecoilEnabled && Camera.main != null)
                { recoilCam = Camera.main.transform; recoilT = 0f; recoilOn = true; camBase = recoilCam.localPosition; }
            }
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            if (flash != null)
            {
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.4f, p);
                var c = flash.color; c.a = 1f - p; flash.color = c;
            }
            for (int i = 0; i < lines; i++)
            {
                float ang = (360f / lines) * i * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                rays[i].SetPosition(0, transform.position + dir * radius * p * 0.35f);
                rays[i].SetPosition(1, transform.position + dir * radius * Mathf.Min(1f, p * 1.6f));
                var sc = rays[i].startColor; sc.a = 1f - p; rays[i].startColor = sc;
            }
            // 해제 반동: unscaled 감쇠 진동, 종료 시 원위치 복원
            if (recoilOn && recoilCam != null && cfgRef != null)
            {
                recoilT += Time.unscaledDeltaTime;
                float rp = Mathf.Clamp01(recoilT / Mathf.Max(0.01f, cfgRef.clashRecoilTime));
                if (rp >= 1f) { recoilCam.localPosition = camBase; recoilOn = false; }
                else
                {
                    float amp = cfgRef.clashRecoilAmp * (1f - rp);
                    recoilCam.localPosition = camBase + (Vector3)(UnityEngine.Random.insideUnitCircle * amp);
                }
            }
            if (t >= dur && !recoilOn) { Time.timeScale = 1f; Destroy(gameObject); }
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
