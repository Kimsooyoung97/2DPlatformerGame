using UnityEngine;
using NHNDemo;
using Cainos.PixelArtPlatformer_Dungeon;

/// <summary>
/// 상자를 공격으로 부숴서 여는 처리.
/// 피격 판정은 자식 HitBox(MonsterHealth)가 담당하고, 그 HitBox 가 "죽으면"
/// 이 스크립트가 Chest.Open() 을 호출합니다.
/// MonsterHealth 는 죽을 때 자기 GameObject 를 파괴하므로, 상자 본체가 아니라
/// 자식에 붙여야 상자가 남습니다.
/// </summary>
public class ChestBreakOpen : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Chest chest;
    [Tooltip("피격 판정을 담당하는 자식 오브젝트의 MonsterHealth")]
    [SerializeField] private MonsterHealth hitBox;

    [Header("연출")]
    [Tooltip("맞을 때마다 상자를 살짝 흔듭니다.")]
    [SerializeField] private float shakeAmount = 0.06f;
    [SerializeField] private float shakeSeconds = 0.12f;

    private Vector3 _homePos;
    private bool _opened;

    private void Awake()
    {
        if (chest == null) chest = GetComponent<Chest>();
        _homePos = transform.position;

        if (hitBox == null) hitBox = GetComponentInChildren<MonsterHealth>(true);
        if (hitBox == null) return;

        hitBox.OnHealthChanged += HandleHit;
        hitBox.OnDied += HandleBroken;
    }

    private void OnDestroy()
    {
        if (hitBox == null) return;
        hitBox.OnHealthChanged -= HandleHit;
        hitBox.OnDied -= HandleBroken;
    }

    private void HandleHit(int current, int max)
    {
        if (_opened || current <= 0) return;
        if (shakeAmount <= 0f) return;
        StopAllCoroutines();
        StartCoroutine(Shake());
    }

    private System.Collections.IEnumerator Shake()
    {
        float t = 0f;
        while (t < shakeSeconds)
        {
            t += Time.deltaTime;
            float x = Random.Range(-shakeAmount, shakeAmount);
            transform.position = _homePos + new Vector3(x, 0f, 0f);
            yield return null;
        }
        transform.position = _homePos;
    }

    private void HandleBroken()
    {
        if (_opened) return;
        _opened = true;

        StopAllCoroutines();
        transform.position = _homePos;

        if (chest != null) chest.Open();
        else Debug.LogWarning("[ChestBreakOpen] Chest 참조가 없습니다.");
    }
}
