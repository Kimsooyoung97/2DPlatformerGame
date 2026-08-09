using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // 6번 키: 검기 날리기. 기존 EffectProjectile 프리팹(Skill1/Skill2)을 그대로 재사용한다.
    public class SkillSlashCaster : MonoBehaviour
    {
        public SkillSlotConfig config;
        public GameObject slashPrefab;      // Skill1.prefab 등
        public Sprite[] frames;             // 비우면 프리팹 기본 프레임 사용
        private float lastCast = -999f;
        private PlayerMana mana;
        private SpriteRenderer sr;

        private void Awake()
        {
            mana = GetComponent<PlayerMana>();
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            var kb = PlayerController2D.InputLocked ? null : Keyboard.current;
            if (kb == null || !kb.digit6Key.wasPressedThisFrame) return;
            if (config == null || slashPrefab == null) return;
            if (Time.time - lastCast < config.cooldown) return;
            if (mana != null && !mana.TryUseMp(config.mpCost)) return; // MP 부족 시 불발
            lastCast = Time.time;
            float dir = (sr != null && sr.flipX) ? -1f : 1f;
            var go = Instantiate(slashPrefab,
                transform.position + new Vector3(dir * config.spawnForward, config.spawnHeight, 0f),
                Quaternion.identity);
            go.transform.localScale = Vector3.one * config.scale;
            var ep = go.GetComponent<EffectProjectile>();
            if (ep != null)
                ep.Launch(dir, config.speed, config.life, frames, config.fps, config.damage,
                    config.hitbox2D, config.piercing);
        }
    }
}
