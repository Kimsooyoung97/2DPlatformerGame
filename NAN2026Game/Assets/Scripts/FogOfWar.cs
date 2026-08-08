using UnityEngine;
using UnityEngine.Tilemaps;
using NAN2026.Core;

namespace NAN2026
{
    // 전장의 안개: 맵을 덮는 어둠 텍스처를 플레이어 접근 시 영구적으로 밝힌다.
    // A안 시야 차폐: 각도별 레이캐스트로 차단 거리를 재고, 막힌 너머는 밝히지 않는다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class FogOfWar : MonoBehaviour
    {
        [SerializeField] private FogOfWarConfig config;
        [SerializeField] private Transform target;

        private Texture2D tex;
        private SpriteRenderer sr;
        private Color32[] pixels;
        private float[] blocked;
        private int w;
        private int h;
        private Vector2 lastStamp;
        private bool hasStamped;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            Vector2 size = config.boundsMax - config.boundsMin;
            w = Mathf.CeilToInt(size.x * config.texelsPerUnit);
            h = Mathf.CeilToInt(size.y * config.texelsPerUnit);
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            pixels = new Color32[w * h];
            blocked = new float[Mathf.Max(1, config.rayCount)];
            var c = new Color32(
                (byte)(config.fogColor.r * 255f),
                (byte)(config.fogColor.g * 255f),
                (byte)(config.fogColor.b * 255f),
                (byte)(config.fogAlpha * 255f));
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels32(pixels);
            tex.Apply(false);
            sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), Vector2.zero, config.texelsPerUnit);
            sr.sortingOrder = config.sortingOrder;
            transform.position = new Vector3(config.boundsMin.x, config.boundsMin.y, 0f);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // 씬마다 플레이어 이름이 달라 수동 배선이 끊기는 사고가 반복됐다.
                // 미배선이면 단일 창구로 자동 탐색한다(태그 → Player → RealPlayer).
                target = NAN2026.PlayerLocator.FindTransform();
                if (target == null) return;
            }
            Vector2 p = (Vector2)target.position + new Vector2(0f, config.eyeHeight);
            if (hasStamped && !FogLogic.ShouldRestamp(p.x - lastStamp.x, p.y - lastStamp.y, config.moveThreshold)) return;
            Stamp(p);
            lastStamp = p;
            hasStamped = true;
        }

        private void Stamp(Vector2 eye)
        {
            float tpu = config.texelsPerUnit;
            float reach = config.revealRadius + config.softEdge;
            int rays = blocked.Length;
            // ① 각도별 차단 거리 측정
            for (int i = 0; i < rays; i++)
            {
                float ang = -Mathf.PI + (i + 0.5f) / rays * (2f * Mathf.PI);
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                // 지형(타일맵·컴포지트)만 시야를 막는다 — 캐릭터·트리거·소품 무시
                blocked[i] = reach + 1f;
                var hits = Physics2D.RaycastAll(eye, dir, reach, config.occlusionMask);
                for (int k = 0; k < hits.Length; k++)
                {
                    var hc = hits[k].collider;
                    if (hc == null || hc.isTrigger) continue;
                    if (!(hc is CompositeCollider2D) && !(hc is TilemapCollider2D)) continue;
                    if (hits[k].distance < blocked[i]) blocked[i] = hits[k].distance;
                }
            }
            // ② 텍셀 밝힘 (가시선 통과분만)
            Vector2 local = eye - config.boundsMin;
            int cx = Mathf.RoundToInt(local.x * tpu);
            int cy = Mathf.RoundToInt(local.y * tpu);
            int r = Mathf.CeilToInt(reach * tpu);
            int x0 = Mathf.Max(0, cx - r), x1 = Mathf.Min(w - 1, cx + r);
            int y0 = Mathf.Max(0, cy - r), y1 = Mathf.Min(h - 1, cy + r);
            byte fogA = (byte)(config.fogAlpha * 255f);
            bool dirty = false;
            for (int y = y0; y <= y1; y++)
            {
                int row = y * w;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / tpu;
                    float dy = (y - cy) / tpu;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float factor = FogLogic.RevealFactor(dist, config.revealRadius, config.softEdge);
                    if (factor <= 0f) continue;
                    if (dist > 0.01f)
                    {
                        int bucket = FogLogic.AngleBucket(dx, dy, rays);
                        if (!FogLogic.VisibleAt(dist, blocked[bucket], config.occlusionTolerance)) continue;
                    }
                    byte targetA = (byte)(fogA * (1f - factor));
                    int idx = row + x;
                    if (pixels[idx].a > targetA)
                    {
                        pixels[idx].a = targetA;
                        dirty = true;
                    }
                }
            }
            if (!dirty) return;
            tex.SetPixels32(pixels);
            tex.Apply(false);
        }
    }
}
