using UnityEngine;
using NAN2026.Showroom;
/// <summary>
/// "3연속 가시 투사체 발사" 패턴에서 사용하는 조준 탄환.
/// 발사 시점 플레이어 위치를 향해 직선으로 날아가고,
/// 플레이어가 IParryReflector를 구현한 상태에서 TryParry()가 true를 반환하면
/// 방향을 반전해 보스에게 되돌아가 껍질을 일시적으로 파괴합니다.
/// </summary>
public class SpikeProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private float damage;
    private float lifeTime = 5f;
    private bool isReflected = false;
    private OrkanBoss owner;

    public void Init(Vector2 dir, float speed, float dmg, OrkanBoss bossOwner)
    {
        velocity = dir.normalized * speed;
        damage = dmg;
        owner = bossOwner;

        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;
        }
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 반사된 투사체가 보스 본체에 맞았을 때 -> 껍질 일시 파괴
        if (isReflected)
        {
            var boss = other.GetComponent<OrkanBoss>();
            if (boss != null)
            {
                boss.OnProjectileReflectedHit();
                Destroy(gameObject);
                return;
            }
        }

        if (!isReflected && other.CompareTag("Player"))
        {
            var reflector = other.GetComponent<IParryReflector>();
            if (reflector != null && reflector.TryParry(owner != null ? owner.gameObject : null))
            {
                Reflect();
                return;
            }

            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage, transform.position);

            Destroy(gameObject);
        }
    }

    private void Reflect()
    {
        isReflected = true;
        velocity = -velocity * 1.3f; // 반사되면 살짝 더 빠르게 되돌아감
        lifeTime = 5f;
    }
}
