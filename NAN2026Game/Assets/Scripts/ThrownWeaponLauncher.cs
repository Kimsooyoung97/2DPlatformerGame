using UnityEngine;
using System.Collections;

namespace NAN2026
{
    // 투척 발사기: 대기→전조(철컥)→발사→쿨다운. dir은 스프라이트 좌우로 결정(-1=왼쪽 발사)
    public class ThrownWeaponLauncher : MonoBehaviour
    {
        public ThrownTrapConfig config;
        public ThrownKind kind;
        public Sprite projectileSprite;
        public int dir = -1;
        private Transform player;
        private float nextReady;
        private AudioSource src;

        private void Start()
        {
            var p = GameObject.Find("Player");
            if (p != null) player = p.transform;
            src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f; src.volume = 0.8f;
        }

        private void Update()
        {
            if (config == null || player == null || Time.time < nextReady) return;
            if (Mathf.Abs(player.position.x - transform.position.x) > config.aggroX) return;
            float cd = kind == ThrownKind.Arrow ? config.arrowCooldown : kind == ThrownKind.Shuriken ? config.shurikenCooldown : config.axeCooldown;
            nextReady = Time.time + cd;
            StartCoroutine(FireSeq());
        }

        private IEnumerator FireSeq()
        {
            if (config.sndTelegraph != null) src.PlayOneShot(config.sndTelegraph);
            yield return new WaitForSeconds(config.telegraphTime);
            int shots = kind == ThrownKind.Shuriken ? config.shurikenBurst : 1;
            for (int i = 0; i < shots; i++)
            {
                Fire();
                if (i < shots - 1) yield return new WaitForSeconds(config.shurikenInterval);
            }
        }

        private void Fire()
        {
            var go = new GameObject(kind + "_투사체");
            var sr = go.AddComponent<SpriteRenderer>(); sr.sharedMaterial = NAN2026.FxUnlit.Mat;
            sr.sprite = projectileSprite;
            sr.sortingOrder = 45;
            go.transform.position = transform.position + new Vector3(dir * 0.5f, 0.35f, 0f);
            var pr = go.AddComponent<ThrownProjectile>();
            pr.config = config; pr.kind = kind; pr.launcher = gameObject;
            Vector2 v;
            if (kind == ThrownKind.Arrow) { v = new Vector2(dir * config.arrowSpeed, 0f); if (config.sndArrow != null) src.PlayOneShot(config.sndArrow); }
            else if (kind == ThrownKind.Shuriken) v = new Vector2(dir * config.shurikenSpeed, 0f);
            else v = new Vector2(dir * config.axeSpeed, config.axeUpVel);
            pr.Launch(v);
        }
    }
}
