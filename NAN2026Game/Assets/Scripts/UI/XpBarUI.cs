using UnityEngine;
using UnityEngine.UI;

namespace NAN2026
{
    /// <summary>
    /// Image(Image Type = Filled)를 경험치 바로 쓰기 위한 구독자.
    /// PlayerProgression.OnXpChanged 이벤트를 구독해서 fillAmount를 갱신한다.
    ///
    /// RealPlayer(및 PlayerProgression)는 PersistentSingleton으로 DontDestroyOnLoad돼있어서
    /// Awake/Start가 게임 전체에서 한 번만 돈다. 반면 이 UI 오브젝트는 씬마다 새로 생기거나
    /// 나중에 활성화될 수 있으므로, OnEnable에서 이벤트 구독과 별개로 현재 값을 한 번
    /// 직접 읽어와 즉시 동기화한다 — 그래야 "이벤트를 놓친 구간" 없이 항상 최신 상태로 시작한다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class XpBarUI : MonoBehaviour
    {
        [Tooltip("비워두면 PlayerLocator로 자동으로 찾는다.")]
        [SerializeField] private PlayerProgression progression;

        private Image bar;

        private void Awake()
        {
            bar = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (progression == null)
            {
                GameObject player = PlayerLocator.Find();
                if (player != null) progression = player.GetComponent<PlayerProgression>();
            }
            if (progression == null)
            {
                Debug.LogWarning("[XpBarUI] PlayerProgression을 못 찾았습니다: " + gameObject.name, this);
                return;
            }

            progression.OnXpChanged += HandleXpChanged;
            HandleXpChanged(progression.Xp, progression.XpToNextLevel); // 구독 시점에 즉시 동기화
        }

        private void OnDisable()
        {
            if (progression != null)
                progression.OnXpChanged -= HandleXpChanged;
        }

        private void HandleXpChanged(int currentXp, int xpToNextLevel)
        {
            if (bar == null) return;
            bar.fillAmount = xpToNextLevel > 0 ? Mathf.Clamp01((float)currentXp / xpToNextLevel) : 0f;
        }
    }
}