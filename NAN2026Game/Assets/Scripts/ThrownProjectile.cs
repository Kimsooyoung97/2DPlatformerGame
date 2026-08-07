using UnityEngine;
using System.Reflection;

namespace NAN2026
{
    public enum ThrownKind { Arrow, Shuriken, Axe }

    // 투척 투사체: 가로 직선 비행, 자체 발광, 통일 패링(TryParry) 판정
    public class ThrownProjectile : MonoBehaviour
    {
        public ThrownTrapConfig config;
        public ThrownKind kind;
        public GameObject launcher;
        private Vector2 vel;
        private float spin, born;
        private Transform player;
        private Component controller;
        private MethodInfo tryParry;
        private bool reflected;

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
            spin = kind == ThrownKind.Arrow ? 0f : config.ballSpin;
            if (kind == ThrownKind.Arrow)
                transform.rotation = Quaternion.Euler(0f, 0f, vel.x >= 0f ? -90f : 90f); // 세로 화살 그림을 진행방향으로
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(vel * dt);
            if (spin != 0f) transform.Rotate(0f, 0f, -Mathf.Sign(vel.x) * spin * dt);
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
                if (r is bool && (bool)r) { OnParried(); return; }
            }
            if (dist <= 0.55f)
            {
                player.SendMessage("TakeDamage", config.damage, SendMessageOptions.DontRequireReceiver);
                Destroy(gameObject);
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
                vel = dir * config.reflectSpeed;
                return;
            }
            Destroy(gameObject);
        }
    }
}
