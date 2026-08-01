using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblingPlatform : MonoBehaviour
    {
        [SerializeField] private PlatformConfig config;

        private SpriteRenderer sr;
        private BoxCollider2D box;
        private float triggerTime = -1f;
        private readonly Collider2D[] overlapBuf = new Collider2D[4];

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            box = GetComponent<BoxCollider2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggerTime >= 0f) return;
            if (collision.collider.GetComponent<PlayerController2D>() == null) return;
            if (collision.collider.bounds.min.y < box.bounds.max.y - 0.25f) return;
            triggerTime = Time.time;
        }

        private void Update()
        {
            if (config == null || triggerTime < 0f) return;
            float t = Time.time - triggerTime;
            int phase = PlayerLocomotionLogic.CrumblePhase(t, config.disappearDelay, config.respawnDelay);
            if (phase == 1)
            {
                bool visible = Mathf.FloorToInt(t * config.blinkHz * 2f) % 2 == 0;
                var c = sr.color;
                c.a = visible ? 1f : 0.35f;
                sr.color = c;
            }
            else if (phase == 2)
            {
                if (sr.enabled) { sr.enabled = false; box.enabled = false; }
            }
            else if (phase == 3)
            {
                int n = Physics2D.OverlapBoxNonAlloc(box.bounds.center, box.size, 0f, overlapBuf);
                for (int i = 0; i < n; i++)
                    if (overlapBuf[i] != null && overlapBuf[i].GetComponent<PlayerController2D>() != null) return;
                sr.enabled = true;
                box.enabled = true;
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
                triggerTime = -1f;
            }
        }
    }
}