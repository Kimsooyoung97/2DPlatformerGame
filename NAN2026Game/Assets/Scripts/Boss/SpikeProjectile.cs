using UnityEngine;
using NAN2026.Showroom;
/// <summary>
/// "3연속 가시 투사체 발사" 패턴에서 사용하는 조준 탄환.
/// 발사 시점 플레이어 위치를 향해 직선으로 날아가고,
/// 플레이어가 IParryReflector를 구현한 상태에서 TryParry()가 true를 반환하면
/// 방향을 반전해 쏜 주인(ownerHealth)에게 되돌아가 데미지를 입힌다.
/// 특정 보스 타입에 묶이지 않도록 owner는 NHNDemo.MonsterHealth로 일반화되어 있어
/// 어떤 몬스터든 이 투사체를 재사용할 수 있다.
/// </summary>
public class SpikeProjectile : MonoBehaviour
{
    private Vector2 velocity;
    private float damage;
    private float lifeTime = 5f;
    private bool isReflected = false;
    private NHNDemo.MonsterHealth ownerHealth;

    public void Init(Vector2 dir, float speed, float dmg, NHNDemo.MonsterHealth owner)
    {
        velocity = dir.normalized * speed;
        damage = dmg;
        ownerHealth = owner;

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
        // 반사된 투사체가 쏜 주인에게 맞았을 때 -> 주인이 대신 데미지를 입는다.
        if (isReflected)
        {
            NHNDemo.MonsterHealth hitHealth = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (hitHealth != null && hitHealth == ownerHealth)
            {
                hitHealth.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)), (Vector2)velocity.normalized);
                Destroy(gameObject);
                return;
            }
        }

        if (!isReflected && other.CompareTag("Player"))
        {
            var reflector = other.GetComponentInParent<IParryReflector>();
            if (reflector != null && reflector.TryParry(ownerHealth != null ? ownerHealth.gameObject : null))
            {
                Reflect();
                return;
            }

            var hp = other.GetComponentInParent<PlayerHealth>();
            if (hp != null)
                hp.TakeDamage(damage);

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
