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

        /// <summary>MP 소모까지 통과해 실제로 캐스트가 확정된 순간에만 1회 발생.
        /// PlayerSoundPlayer가 구독해 스킬 사운드를 재생한다.</summary>
        public static event System.Action OnSkill1Performed;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
        }

        private void Update()
        {
            var kb = PlayerController2D.InputLocked ? null : Keyboard.current;
            if (kb == null || !kb.digit1Key.wasPressedThisFrame) return;
            if (!NAN2026.SkillGate.IsUnlocked(0)) return;      // 상자에서 아이콘을 먹어야 사용 가능
            if (casting || Time.time - lastCast < config.cooldown) return;
            NAN2026.SkillGate.Report(0, config.cooldown);      // 아이콘 쿨타임 표시용
            var mana = GetComponent<NAN2026.PlayerMana>();
            if (mana != null && !mana.TryUseMp(config.mpCost)) return; // MP 부족 시 불발
            OnSkill1Performed?.Invoke();
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
                        var pR = transform.position + new Vector3(ox, 0f, 0f);
                        var pL = transform.position + new Vector3(-ox, 0f, 0f);
                        SpawnEffect(pR); DamageAround(pR);
                        SpawnEffect(pL); DamageAround(pL);
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

        // 번개가 내리꽂힌 지점 주변의 적을 때린다 (보스·일반 몬스터 공통)
        private void DamageAround(Vector3 center)
        {
            var hits = Physics2D.OverlapBoxAll(center, config.hitSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null) continue;
                if (h.GetComponentInParent<PlayerHealth>() != null) continue; // 자신 제외
                var mino = h.GetComponentInParent<NAN2026.MinoBoss>();
                if (mino != null) { mino.TakeDamage(config.damage); continue; }
                var demon = h.GetComponentInParent<NAN2026.DemonBoss>();
                if (demon != null) { demon.TakeDamage(config.damage); continue; }
                var mon = h.GetComponentInParent<NHNDemo.MonsterHealth>();
                // SendMessage는 인자 1개만 넘긴다. TakeDamage(int, Vector2)는 2개라 직접 호출해야 한다
                // (SendMessage 사용 시 'Failed to call function TakeDamage' 에러 → Error Pause로 에디터 정지)
                if (mon != null)
                    mon.TakeDamage(config.damage, new Vector2(Mathf.Sign(center.x - transform.position.x), 0.2f));
            }
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
                { if (hit.collider.isTrigger || hit.collider.transform.root == transform.root) continue; groundY = hit.point.y; break; } // 최근접 표면(공중 발판 포함)
                break;
            }
            // 폴백: 전방 지점이 좁은 발판을 벗어난 경우 발밑 x 재캐스트
            float feetY = transform.position.y;
            if (float.IsNaN(groundY) || groundY < feetY - config.platformMissTolerance)
            {
                var o2 = new Vector2(transform.position.x, transform.position.y + 0.5f);
                float g2 = float.NaN;
                foreach (var hit in Physics2D.RaycastAll(o2, Vector2.down, config.groundSnapDepth))
                { if (hit.collider.isTrigger || hit.collider.transform.root == transform.root) continue; g2 = hit.point.y; break; }
                if (!float.IsNaN(g2)) { groundY = g2; pos.x = transform.position.x; }
            }
            if (float.IsNaN(groundY)) { groundY = feetY; pos.x = transform.position.x; } // 최후: 발 높이 시전
            var go = new GameObject("SkillSlash_Effect");
            pos.y = groundY;
            if (effectSprites != null && effectSprites.Length > 0)
                pos.y += effectSprites[0].bounds.extents.y * config.effectScale;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * config.effectScale;
            var esr = go.AddComponent<SpriteRenderer>(); esr.sharedMaterial = NAN2026.FxUnlit.Mat;
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