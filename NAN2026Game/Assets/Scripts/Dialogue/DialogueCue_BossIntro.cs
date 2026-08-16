using UnityEngine;
using NAN2026;

/// <summary>
/// DemonBoss 의 등장(변신) 인트로가 끝나는 순간 DialogueTrigger 를 발동시키는 다리 역할.
///
/// 보스는 BossEncounterTrigger 가 아레나 진입 시점에 켜주기 전까지 비활성이다.
/// 비활성 오브젝트의 컴포넌트에도 이벤트 구독은 정상 동작하므로, 씬 시작 시점에 미리 걸어둔다.
///
/// 리트라이(ResetBoss)로 인트로가 다시 재생되면 이벤트도 다시 오지만,
/// 대사를 한 번만 보이고 싶으면 DialogueTrigger 의 재생조건을 OncePerSession 으로 둔다.
/// </summary>
[RequireComponent(typeof(DialogueTrigger))]
public class DialogueCue_BossIntro : MonoBehaviour
{
    [Tooltip("등장 연출이 끝나는 것을 감시할 보스")]
    [SerializeField] private DemonBoss boss;

    [Tooltip("비우면 같은 오브젝트의 DialogueTrigger 를 쓴다. 트리거의 발동 방식은 Manual 이어야 한다")]
    [SerializeField] private DialogueTrigger trigger;

    private bool _subscribed;

    private void Awake()
    {
        if (trigger == null) trigger = GetComponent<DialogueTrigger>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || boss == null) return;
        boss.OnIntroFinished += HandleIntroFinished;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || boss == null) return;
        boss.OnIntroFinished -= HandleIntroFinished;
        _subscribed = false;
    }

    private void HandleIntroFinished()
    {
        if (trigger == null)
        {
            Debug.LogWarning("[DialogueCue_BossIntro] DialogueTrigger 미배선", this);
            return;
        }
        trigger.PlayNow();
    }
}
