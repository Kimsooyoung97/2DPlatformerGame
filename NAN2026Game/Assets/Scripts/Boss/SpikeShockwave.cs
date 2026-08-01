using UnityEngine;

/// <summary>
/// "가시 철퇴 내리치기" 패턴에서 좌우로 퍼져나가는 충격파 가시 하나.
/// 보스가 내리치는 지점 기준으로 왼쪽/오른쪽에 각각 하나씩 생성해서 사용합니다.
/// 별도 아트가 준비되면 이 오브젝트에 SpriteRenderer/Animator만 붙이면 됩니다.
/// </summary>
public class SpikeShockwave : MonoBehaviour
{
    private float speed;
    private float maxDistance;
    private float damage;
    private Vector3 startPos;
    private int direction = 1; // 1: 오른쪽, -1: 왼쪽

    private bool hasHitPlayer = false;

    public void Init(int dir, float moveSpeed, float range, float dmg)
    {
        direction = dir;
        speed = moveSpeed;
        maxDistance = range;
        damage = dmg;
        startPos = transform.position;

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.0f, 1.2f);
        }
        else
        {
            col.isTrigger = true;
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        if (Vector3.Distance(startPos, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitPlayer) return;

        if (other.CompareTag("Player"))
        {
            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.TakeDamage(damage, transform.position);
                hasHitPlayer = true;
            }
        }
    }
}
