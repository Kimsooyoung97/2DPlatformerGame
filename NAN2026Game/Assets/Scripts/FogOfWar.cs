using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    // 전장의 안개: 맵을 덮는 어둠 텍스처를 플레이어 접근 시 영구적으로 밝힌다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class FogOfWar : MonoBehaviour
    {
        [SerializeField] private FogOfWarConfig config;
        [SerializeField] private Transform target;

        private Texture2D tex;
        private SpriteRenderer sr;
        private Color32[] pixels;
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
            if (target == null) return;
            Vector2 p = target.position;
            if (hasStamped && !FogLogic.ShouldRestamp(p.x - lastStamp.x, p.y - lastStamp.y, config.moveThreshold)) return;
            Stamp(p);
            lastStamp = p;
            hasStamped = true;
        }

        private void Stamp(Vector2 worldPos)
        {
            float tpu = config.texelsPerUnit;
            Vector2 local = worldPos - config.boundsMin;
            float reach = config.revealRadius + config.softEdge;
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
