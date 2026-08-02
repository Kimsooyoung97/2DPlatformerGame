using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어 HP. 전역 네임스페이스 — 팀 스크립트(OrkanBoss·Spike·Checkpoint2D·OrbProjectile) 계약 준수.
// 사망: 체크포인트 있으면 그 지점 부활, 없으면 씬 재시작 (SPEC: 죽으면 처음부터)
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHp;
    [SerializeField] private float blinkInterval;
    [SerializeField] private int blinkCount;
    private float hp;
    private SpriteRenderer sr;
    private bool invulnerable;
    private Vector3 checkpoint;
    private bool hasCheckpoint;

    public float Hp { get { return hp; } }

    private void Awake()
    {
        hp = maxHp;
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetCheckpoint(Vector3 pos)
    {
        checkpoint = pos;
        hasCheckpoint = true;
    }

    public void TakeDamage(float amount)
    {
        if (invulnerable) return;
        hp -= amount;
        if (hp <= 0) { Kill(); return; }
        StartCoroutine(Blink());
    }

    public void TakeDamage(float amount, Vector3 sourcePos)
    {
        TakeDamage(amount);
    }

    public void Kill()
    {
        if (hasCheckpoint)
        {
            hp = maxHp;
            transform.position = checkpoint;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            StartCoroutine(Blink());
            return;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator Blink()
    {
        invulnerable = true;
        for (int i = 0; i < blinkCount; i++)
        {
            sr.enabled = false;
            yield return new WaitForSeconds(blinkInterval);
            sr.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
        }
        invulnerable = false;
    }
}
