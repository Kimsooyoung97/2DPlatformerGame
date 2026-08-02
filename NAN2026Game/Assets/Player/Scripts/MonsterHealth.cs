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

        /// <summary>머리 위 체력바(WorldHealthBar 등)가 즉시 동기화할 수 있도록
        /// 체력이 바뀌는 때마다 (현재, 최대)를 통지한다.</summary>
        public event System.Action<int, int> OnHealthChanged;

        /// <summary>이 몬스터가 죽는 순간(페이드 시작 전) 한 번 호출된다. 경험치 지급 등에 사용.</summary>
        public event System.Action OnDied;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
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

            foreach (Collider2D item in GetComponentsInChildren<Collider2D>())
                item.enabled = false;

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
