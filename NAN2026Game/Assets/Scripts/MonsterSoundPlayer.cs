using UnityEngine;
using NHNDemo;

/// <summary>
/// 몬스터 피격/사망 사운드. MonsterHealth 의 이벤트를 구독하므로
/// MonsterHealth.cs 를 수정하지 않고 컴포넌트만 붙이면 동작합니다.
/// 클립을 비워두면 그 소리는 나지 않습니다.
/// </summary>
[RequireComponent(typeof(MonsterHealth))]
public class MonsterSoundPlayer : MonoBehaviour
{
    [Header("피격 사운드")]
    [SerializeField] private AudioClip hitClip;
    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 0.7f;
    [Tooltip("재생마다 음정을 ±값만큼 무작위로 흔듭니다. 연타 시 단조로움을 줄입니다.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float hitPitchJitter = 0.08f;

    [Header("사망 사운드")]
    [SerializeField] private AudioClip deathClip;
    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 0.85f;
    [SerializeField] private float deathPitch = 1f;

    private MonsterHealth _health;
    private int _lastHealth = int.MinValue;
    private bool _died;

    private void Awake()
    {
        _health = GetComponent<MonsterHealth>();
        if (_health == null) return;

        _lastHealth = _health.CurrentHealth;
        _health.OnHealthChanged += HandleHealthChanged;
        _health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (_health == null) return;
        _health.OnHealthChanged -= HandleHealthChanged;
        _health.OnDied -= HandleDied;
    }

    private void HandleHealthChanged(int current, int max)
    {
        // 체력이 줄었고 아직 살아 있을 때만 피격음
        bool damaged = _lastHealth != int.MinValue && current < _lastHealth;
        _lastHealth = current;

        if (!damaged || current <= 0 || _died) return;
        if (hitClip == null) return;

        float p = 1f + Random.Range(-hitPitchJitter, hitPitchJitter);
        PlayDetached(hitClip, hitVolume, p);
    }

    private void HandleDied()
    {
        if (_died) return;
        _died = true;
        if (deathClip == null) return;
        PlayDetached(deathClip, deathVolume, deathPitch);
    }

    /// <summary>
    /// 몬스터가 사라진 뒤에도 소리가 끝까지 나도록, 별도 오브젝트를 만들어 재생합니다.
    /// 몬스터 자신의 AudioSource 로 재생하면 파괴될 때 소리가 잘립니다.
    /// </summary>
    private void PlayDetached(AudioClip clip, float volume, float pitch)
    {
        var go = new GameObject("MonsterSFX_" + clip.name);
        go.transform.position = transform.position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f);
    }
}
