using UnityEngine;
using System.Reflection;

namespace NAN2026
{
    public enum ThrownKind { Arrow, Shuriken, Axe }

    // 투척 투사체: 비행·회전, 통일 패링(TryParry) 판정, 착탄·반사
    public class ThrownProjectile : MonoBehaviour
    {
        public ThrownTrapConfig config;
        public ThrownKind kind;
        public GameObject launcher; // 도끼 반사 파괴 대상
        private Vector2 vel;
        private float spin, born;
        private Transform player;
        private Component controller;
        private MethodInfo tryParry;
        private bool reflected;
        private AudioSource loopSrc;

        public void Launch(Vector2 v)
        {
            vel = v; born = Time.time;
            var p = GameObject.Find("Player");
            if (p != null)
            {
                player = p.transform;
                foreach (var mb in p.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }
            spin = kind == ThrownKind.Shuriken ? config.shurikenSpin : kind == ThrownKind.Axe ? config.axeSpin : 0f;
            if (kind != ThrownKind.Arrow && config.sndSpin != null)
            {
                loopSrc = gameObject.AddComponent<AudioSource>();
                loopSrc.clip = config.sndSpin; loopSrc.loop = true; loopSrc.spatialBlend = 0f; loopSrc.volume = 0.6f;
                loopSrc.Play();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (kind == ThrownKind.Axe) vel.y -= 9.81f * config.axeGravity * dt;
            transform.position += (Vector3)(vel * dt);
            if (spin != 0f) transform.Rotate(0f, 0f, -Mathf.Sign(vel.x) * spin * dt);
            else transform.right = new Vector3(Mathf.Sign(vel.x), 0f, 0f) * -1f; // 화살촉이 진행방향
            if (Time.time - born > config.lifeTime) { Destroy(gameObject); return; }

            if (reflected)
            {
                if (launcher != null && Vector2.Distance(transform.position, launcher.transform.position) < 0.8f)
                {
                    if (config.sndLauncherBreak != null) AudioSource.PlayClipAtPoint(config.sndLauncherBreak, transform.position, 0.9f);
                    Destroy(launcher);
                    Destroy(gameObject);
                }
                return;
            }
            if (player == null) return;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= config.parryReach && controller != null && tryParry != null)
            {
                object r = tryParry.Invoke(controller, new object[] { gameObject });
                if (r is bool && (bool)r)
                {
                    OnParried();
                    return;
                }
            }
            if (dist <= 0.55f)
            {
                player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                Impact();
            }
        }

        private void OnParried()
        {
            int mp = kind == ThrownKind.Arrow ? config.arrowMp : kind == ThrownKind.Shuriken ? config.shurikenMp : config.axeMp;
            if (player != null) player.SendMessage("AddMp", mp, SendMessageOptions.DontRequireReceiver);
            if (kind == ThrownKind.Axe && launcher != null)
            {
                reflected = true;
                Vector2 dir = ((Vector2)launcher.transform.position - (Vector2)transform.position).normalized;
                vel = dir * config.axeReflectSpeed;
                return;
            }
            Destroy(gameObject);
        }

        private void Impact()
        {
            if (kind == ThrownKind.Axe && config.sndAxeImpact != null)
                AudioSource.PlayClipAtPoint(config.sndAxeImpact, transform.position, 0.9f);
            Destroy(gameObject);
        }
    }
}
