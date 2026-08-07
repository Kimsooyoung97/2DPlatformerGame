using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

namespace NAN2026
{
    // 투척 발사기: 접근 시 가로로 하나씩 발사. 전조음 없음(발사음만), 투사체는 발광
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
            src.spatialBlend = 0f; src.volume = 0.85f;
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
            yield return new WaitForSeconds(config.telegraphTime);
            Fire();
        }

        private void Fire()
        {
            if (config.sndFire != null) src.PlayOneShot(config.sndFire);
            var go = new GameObject(kind + "_투사체");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite;
            sr.sharedMaterial = NAN2026.FxUnlit.Mat;
            sr.color = config.glowColor;
            sr.sortingOrder = 45;
            go.transform.position = transform.position + new Vector3(dir * 0.6f, 0.3f, 0f);
            var lt = go.AddComponent<Light2D>();
            lt.lightType = Light2D.LightType.Point;
            lt.intensity = config.glowIntensity;
            lt.pointLightOuterRadius = config.glowRadius;
            lt.color = config.glowColor;
            var pr = go.AddComponent<ThrownProjectile>();
            pr.config = config; pr.kind = kind; pr.launcher = gameObject;
            float sp = kind == ThrownKind.Arrow ? config.arrowSpeed : kind == ThrownKind.Shuriken ? config.shurikenSpeed : config.axeSpeed;
            pr.Launch(new Vector2(dir * sp, 0f));
        }
    }
}
