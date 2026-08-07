using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    // 준보스: 대기→추격(run)→sp_atk, 타격 순간 패링 판정(구체와 동일 TryParry 경로)
    public class MidBossAI : MonoBehaviour
    {
        [SerializeField] private MidBossConfig config;
        [SerializeField] private Transform player;
        Animator anim; SpriteRenderer sr;
        MonoBehaviour controller; System.Reflection.MethodInfo tryParry;
        string state = "";
        float atkT = -1f; bool hitDone; float cooldownUntil;

        void Start()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            if (player != null)
            {
                foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }
        }

        void Update()
        {
            if (config == null || player == null) return;
            if (atkT >= 0f) { RunAttack(); return; }
            float dist = Mathf.Abs(transform.position.x - player.position.x);
            int phase = MidBossLogic.Phase(dist, config.aggroRange, config.attackRange);
            if (phase == 2 && Time.time >= cooldownUntil)
            {
                atkT = 0f; hitDone = false;
                Face(); Play("SpAtk");
            }
            else if (phase >= 1)
            {
                Face();
                float dir = player.position.x > transform.position.x ? 1f : -1f;
                if (phase == 1) transform.position += new Vector3(dir * config.walkSpeed * Time.deltaTime, 0f, 0f);
                Play(phase == 1 ? "Run" : "MidBoss_Idle");
            }
            else Play("MidBoss_Idle");
        }

        void RunAttack()
        {
            atkT += Time.deltaTime;
            bool inStrike = MidBossLogic.InStrikeInterval(atkT, config.attackDuration, config.hitFrac, config.hitFracEnd);
            float dist = Vector2.Distance(transform.position, player.position);
            // 통일 패링: 구간 내 리치 접촉 + 창 활성이면 언제든 성공
            if (!hitDone && inStrike && dist <= config.hitReach && controller != null && tryParry != null)
            {
                object r = tryParry.Invoke(controller, new object[] { gameObject });
                if (r is bool && (bool)r)
                {
                    hitDone = true;
                    SpikeBallTrap.ShowAt(player.position + Vector3.up * 1.4f, "패링 성공!", new Color(0.35f, 1f, 0.45f), config.clashConfig);
                    Vector3 mid = (transform.position + player.position) * 0.5f + Vector3.up * 0.8f;
                    if (config.clashConfig != null)
                    {
                        ParryClashFx.Play(mid, config.clashConfig);
                        if (config.clashConfig.clashSound != null)
                            ClashSfx.PlaySegment(config.clashConfig.clashSound, config.clashConfig.clashVolume, config.clashConfig.clashSoundStartMs, config.clashConfig.clashSoundEndMs);
                    }
                }
            }
            // 구간 종료 순간: 못 쳐냈고 리치 안이면 피해
            if (!hitDone && atkT / Mathf.Max(0.01f, config.attackDuration) > config.hitFracEnd)
            {
                hitDone = true;
                if (dist <= config.hitReach)
                {
                    SpikeBallTrap.ShowAt(player.position + Vector3.up * 1.4f, "패링 실패!", new Color(1f, 0.3f, 0.25f), config.clashConfig);
                    player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (atkT >= config.attackDuration)
            { atkT = -1f; cooldownUntil = Time.time + config.attackCooldown; Play("MidBoss_Idle"); }
        }

        LineRenderer[] rangeRings;
        void LateUpdate()
        {
            if (config == null) return;
            if (config.showRangesInGame && rangeRings == null)
            {
                rangeRings = new LineRenderer[3];
                var cols = new[] { new Color(1f, 0.9f, 0.2f, 0.5f), new Color(1f, 0.25f, 0.25f, 0.6f), new Color(1f, 0.3f, 1f, 0.6f) };
                for (int r = 0; r < 3; r++)
                {
                    var go = new GameObject("RangeRing" + r);
                    go.transform.SetParent(transform, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 48;
                    lr.startWidth = 0.05f; lr.endWidth = 0.05f;
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.startColor = cols[r]; lr.endColor = cols[r];
                    lr.sortingOrder = 850;
                    rangeRings[r] = lr;
                }
            }
            if (rangeRings != null)
            {
                if (!config.showRangesInGame)
                { foreach (var lr in rangeRings) if (lr != null) Destroy(lr.gameObject); rangeRings = null; return; }
                float[] rad = { config.aggroRange, config.attackRange, config.hitReach };
                float inv = transform.localScale.x != 0f ? 1f / Mathf.Abs(transform.localScale.x) : 1f; // 부모 스케일 상쇄
                for (int r = 0; r < 3; r++)
                {
                    var lr = rangeRings[r];
                    for (int i = 0; i < 48; i++)
                    {
                        float a = i / 48f * Mathf.PI * 2f;
                        lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rad[r] * inv, Mathf.Sin(a) * rad[r] * inv, 0f));
                    }
                }
            }
        }

        // 씬 뷰 시각화: 노랑=감지 / 빨강=공격개시 / 자홍=타격리치
        void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.aggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.attackRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, config.hitReach);
        }

        void Face() { if (sr != null) sr.flipX = player.position.x < transform.position.x; }
        void Play(string s2) { if (state == s2 || anim == null) return; state = s2; anim.Play(s2, 0, 0f); }
    }
}
