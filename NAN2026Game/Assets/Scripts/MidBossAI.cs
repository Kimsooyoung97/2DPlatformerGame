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
            if (!hitDone && MidBossLogic.HitMomentPassed(atkT, config.attackDuration, config.hitFrac))
            {
                hitDone = true;
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= config.hitReach)
                {
                    bool ok = false;
                    if (controller != null && tryParry != null)
                    {
                        object r = tryParry.Invoke(controller, new object[] { gameObject });
                        ok = r is bool && (bool)r;
                    }
                    SpikeBallTrap.ShowAt(player.position + Vector3.up * 1.4f, ok ? "패링 성공!" : "패링 실패!", ok ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.3f, 0.25f), config.clashConfig);
                    if (ok)
                    {
                        Vector3 mid = (transform.position + player.position) * 0.5f + Vector3.up * 0.8f;
                        if (config.clashConfig != null)
                        {
                            ParryClashFx.Play(mid, config.clashConfig);
                            if (config.clashConfig.clashSound != null)
                                ClashSfx.PlaySegment(config.clashConfig.clashSound, config.clashConfig.clashVolume, config.clashConfig.clashSoundStartMs, config.clashConfig.clashSoundEndMs);
                        }
                    }
                    else player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (atkT >= config.attackDuration)
            { atkT = -1f; cooldownUntil = Time.time + config.attackCooldown; Play("MidBoss_Idle"); }
        }

        void Face() { if (sr != null) sr.flipX = player.position.x < transform.position.x; }
        void Play(string s2) { if (state == s2 || anim == null) return; state = s2; anim.Play(s2, 0, 0f); }
    }
}
