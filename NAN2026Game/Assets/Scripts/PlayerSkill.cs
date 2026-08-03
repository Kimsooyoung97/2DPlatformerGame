using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    // 1키 스킬: 스킬대기 모션 재생, 지정 프레임 시작 시 양옆으로 내려찍기 이펙트 N개씩 소환.
    // skillSprites가 비어 있으면 포즈 없이 타이밍만 진행 (시트 후속 연결 대비).
    public class PlayerSkill : MonoBehaviour
    {
        [SerializeField] private PlayerSkillConfig config;
        [SerializeField] private Sprite[] skillSprites;   // 기사_스킬대기 프레임 (후속 연결)
        [SerializeField] private Sprite[] effectSprites;  // Effect_1 프레임

        private SpriteRenderer sr;
        private Animator anim;
        private bool casting;
        private float lastCast;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.digit1Key.wasPressedThisFrame) return;
            if (casting || Time.time - lastCast < config.cooldown) return;
            StartCoroutine(Cast());
        }

        private IEnumerator Cast()
        {
            casting = true;
            lastCast = Time.time;
            bool hasPose = skillSprites != null && skillSprites.Length > 0;
            if (hasPose && anim != null) anim.enabled = false;
            int skip = Mathf.Max(0, config.startFrame);
            float triggerTime = SkillLogic.FrameTime(config.triggerFrame - skip, config.skillFps);
            float frameDur = config.skillFps > 0f ? 1f / config.skillFps : 0.1f;
            float t = 0f;
            int spawned = -1;
            int shownFrame = -1;
            float total = hasPose ? (skillSprites.Length - skip) * frameDur : triggerTime + config.sideCount * config.stagger + 0.2f;
            while (t < total)
            {
                if (hasPose)
                {
                    int f = Mathf.Min(skillSprites.Length - 1, skip + (int)(t / frameDur));
                    if (f != shownFrame) { sr.sprite = skillSprites[f]; shownFrame = f; }
                }
                if (t >= triggerTime)
                {
                    int step = (int)((t - triggerTime) / config.stagger);
                    while (spawned < step && spawned < config.sideCount - 1)
                    {
                        spawned++;
                        float ox = SkillLogic.OffsetX(spawned, config.startOffset, config.spacing);
                        SpawnEffect(transform.position + new Vector3(ox, 0f, 0f));
                        SpawnEffect(transform.position + new Vector3(-ox, 0f, 0f));
                    }
                }
                t += Time.deltaTime;
                yield return null;
            }
            // 잔여 미소환분 처리
            while (spawned < config.sideCount - 1)
            {
                spawned++;
                float ox = SkillLogic.OffsetX(spawned, config.startOffset, config.spacing);
                SpawnEffect(transform.position + new Vector3(ox, 0f, 0f));
                SpawnEffect(transform.position + new Vector3(-ox, 0f, 0f));
            }
            if (hasPose && anim != null) anim.enabled = true;
            casting = false;
        }

        private void SpawnEffect(Vector3 pos)
        {
            // 지면 스냅: 해당 x에서 아래로 지형 탐색 — 없으면(구덩이·허공) 이펙트 생략
            float groundY = float.NaN;
            var origin = new Vector2(pos.x, transform.position.y + 0.5f);
            foreach (var hit in Physics2D.RaycastAll(origin, Vector2.down, config.groundSnapDepth))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (!(hit.collider is UnityEngine.Tilemaps.TilemapCollider2D) && !(hit.collider is CompositeCollider2D)) continue;
                groundY = hit.point.y;
                break;
            }
            if (float.IsNaN(groundY)) return;
            var go = new GameObject("SkillSlash_Effect");
            pos.y = groundY;
            if (effectSprites != null && effectSprites.Length > 0)
                pos.y += effectSprites[0].bounds.extents.y * config.effectScale;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * config.effectScale;
            var esr = go.AddComponent<SpriteRenderer>();
            esr.sortingOrder = config.effectSortingOrder;
            var player = go.AddComponent<EffectPlayback>();
            player.Init(effectSprites, config.effectFps);
        }
    }

    // 스프라이트 배열을 1회 재생 후 자멸하는 초경량 이펙트
    public class EffectPlayback : MonoBehaviour
    {
        private Sprite[] frames;
        private float fps;
        private float t;
        private SpriteRenderer sr;

        public void Init(Sprite[] sprites, float framesPerSec)
        {
            frames = sprites;
            fps = framesPerSec;
            sr = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0) { Destroy(gameObject); return; }
            sr.sprite = frames[0];
        }

        private void Update()
        {
            t += Time.deltaTime;
            int f = (int)(t * fps);
            if (f >= frames.Length) { Destroy(gameObject); return; }
            sr.sprite = frames[f];
        }
    }
}
