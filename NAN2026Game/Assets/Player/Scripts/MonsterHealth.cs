using Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts;
using System.Collections;
using UnityEngine;

namespace NHNDemo
{
    public sealed class MonsterHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 4;
        [SerializeField] private float knockbackDistance = 0.12f;

        private int currentHealth;
        private bool dead;
        private SpriteRenderer[] renderers;
        private Color[] originalColors;
        private MonsterAnimation animation;

        /// <summary>머리 위 체력바(WorldHealthBar 등)가 즉시 동기화할 수 있도록
        /// 체력이 바뀌는 때마다 (현재, 최대)를 통지한다.</summary>
        public event System.Action<int, int> OnHealthChanged;

        /// <summary>이 몬스터가 죽는 순간(페이드 시작 전) 한 번 호출된다. 경험치 지급 등에 사용.</summary>
        public event System.Action OnDied;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        /// <summary>몬스터 타입별 Config(EnemyAIConfig 등)가 최대 체력을 강제 적용할 때 사용.
        /// Awake 실행 순서에 관계없이 정확히 동작하도록 maxHealth/currentHealth를 함께 갱신한다.</summary>
        public void SetMaxHealth(int newMax)
        {
            if (newMax <= 0) return;
            maxHealth = newMax;
            currentHealth = newMax;
        }

        private void Awake()
        {
            animation = GetComponent<MonsterAnimation>();
            currentHealth = maxHealth;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[renderers.Length];
            for (int index = 0; index < renderers.Length; index++)
                originalColors[index] = renderers[index].color;
        }

        public void TakeDamage(int damage, Vector2 attackDirection)
        {
            if (dead)
                return;

            currentHealth -= Mathf.Max(1, damage);
            transform.position += (Vector3)(attackDirection.normalized * knockbackDistance);
            StopAllCoroutines();
            StartCoroutine(FlashDamage());

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
                Die();
        }

        private IEnumerator FlashDamage()
        {
            foreach (SpriteRenderer item in renderers)
                item.color = new Color(1f, 0.24f, 0.2f, 1f);

            yield return new WaitForSeconds(0.11f);

            for (int index = 0; index < renderers.Length; index++)
                renderers[index].color = originalColors[index];
        }

        private void Die()
        {
            dead = true;
            OnDied?.Invoke();
            SurfaceMonsterPatrol patrol = GetComponent<SurfaceMonsterPatrol>();
            if (patrol != null)
                patrol.enabled = false;

            animation.Die();

            //foreach (Collider2D item in GetComponentsInChildren<Collider2D>())
            //    item.enabled = false;

            StartCoroutine(FadeAndDestroy());
        }

        private IEnumerator FadeAndDestroy()
        {
            float elapsed = 0f;
            const float duration = 0.45f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - elapsed / duration;
                for (int index = 0; index < renderers.Length; index++)
                {
                    Color color = originalColors[index];
                    color.a = alpha;
                    renderers[index].color = color;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
