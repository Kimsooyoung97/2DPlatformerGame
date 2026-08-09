using Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts;
using System.Collections;
using UnityEngine;

namespace NHNDemo
{
    public sealed class MonsterHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 4;
        [SerializeField] private float knockbackDistance = 0.12f;
        [SerializeField] private float flashDuration = 0.11f;
        [SerializeField] private Color flashColor = new Color(1f, 0.24f, 0.2f, 1f);

        private int currentHealth;
        private bool dead;
        private SpriteRenderer[] renderers;
        private Color[] originalColors;
        private MonsterAnimation animation;

        // FlashDamage를 코루틴(한 번 색 바꾸고 대기 후 복원)으로 하던 걸 이걸로 교체했다.
        // 원인 불명이지만 MonsterAnimation 등 다른 스크립트가 매 프레임 SpriteRenderer.color를
        // 자기 값으로 되돌리는 것으로 추정 — 코루틴은 그 프레임 이후 바로 씹혀서 눈에 안 보였다.
        // MonsterController2D 넉백 때와 같은 전략: 이 시각까지 LateUpdate에서 매 프레임 강제로
        // 색을 덮어써서, 뭐가 됐든 마지막에 쓰는 쪽이 이기게 만든다.
        // flashUntil이 지나는 순간을 감지해서 애니메이터를 다시 켜기 위한 이전 프레임 상태.
        private float flashUntil;
        private bool wasFlashing;

        /// <summary>머리 위 체력바(WorldHealthBar 등)가 즉시 동기화할 수 있도록
        /// 체력이 바뀌는 때마다 (현재, 최대)를 통지한다.</summary>
        public event System.Action<int, int> OnHealthChanged;

        /// <summary>피격 시마다 (데미지, 공격 방향)을 통지한다. EnemyAI 등이 구독해서
        /// MonsterController2D.ApplyKnockback() 같은 실제 물리 넉백을 걸 때 사용한다.</summary>
        public event System.Action<int, Vector2> OnDamaged;

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
            flashUntil = Time.time + flashDuration;
            if (animation != null)
            {
                animation.Hit();
                animation.SetAnimatorEnabled(false); // 히트 포즈에서 정지 — 플래시가 애니메이션에 안 묻히게
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(damage, attackDirection);

            if (currentHealth <= 0)
                Die();
        }

        // Update가 아니라 LateUpdate인 이유: MonsterAnimation 등 다른 컴포넌트가 자기
        // Update()에서 color/스프라이트를 건드리더라도, 그 다음에 실행되는 LateUpdate가
        // 항상 마지막에 덮어써서 깜빡임이 확실히 보이게 만든다.
        private void LateUpdate()
        {
            if (dead) return; // 사망 페이드(FadeAndDestroy)가 알파를 직접 관리하므로 여기서 건드리지 않는다.

            bool flashing = Time.time < flashUntil;
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null) continue;
                renderers[index].color = flashing ? flashColor : originalColors[index];
            }

            // 플래시가 막 끝난 프레임에만 애니메이터를 다시 켠다 — 매 프레임 켜고 끄고 하지 않게.
            if (wasFlashing && !flashing && animation != null)
                animation.SetAnimatorEnabled(true);
            wasFlashing = flashing;
        }

        private void Die()
        {
            dead = true;
            if (animation != null) animation.SetAnimatorEnabled(true); // 플래시 중 사망해도 죽는 애니메이션은 반드시 재생되게
            OnDied?.Invoke();
            SurfaceMonsterPatrol patrol = GetComponent<SurfaceMonsterPatrol>();
            if (patrol != null)
                patrol.enabled = false;

            // MonsterAnimation(PixelFantasy 패키지)이 없는 몬스터도 있어(MidBoss 등
            // 커스텀 애니메이터를 직접 쓰는 경우) 널 체크 후에만 호출한다. 그런 몬스터는
            // 자기 스크립트에서 OnDied 이벤트를 구독해 직접 사망 애니메이션을 튼다.
            if (animation != null) animation.Die();

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