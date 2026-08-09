using System.Collections.Generic;
using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// 이 컴포넌트가 붙은 루트 오브젝트를 DontDestroyOnLoad로 씬 전환 간 유지한다.
    /// 포탈로 다음 씬을 로드했을 때, 그 씬에 손으로 배치된 동일 역할의 오브젝트(예: 그 씬 자체의
    /// RealPlayer/UI Canvas)가 있으면 그쪽을 파괴하고 이미 살아남은 원본을 계속 쓴다 — 이렇게
    /// 해야 중복 생성 없이 정보(체력·MP·인벤토리 등)가 씬을 넘어가도 유지된다.
    ///
    /// 중요: 이 씬 하나에만 붙이는 걸로는 부족하다. 포탈로 이동하는 "모든" 씬에 있는
    /// RealPlayer/UI Canvas 오브젝트에도 같은 singletonId로 이 컴포넌트를 붙여야, 그 씬에
    /// 진입했을 때 중복 판정이 걸려 새로 로드된 쪽이 파괴된다. 한쪽에만 붙이면 다른 씬에서
    /// 두 개가 공존하는 사고가 난다.
    /// </summary>
    public sealed class PersistentSingleton : MonoBehaviour
    {
        [Tooltip("같은 개체를 가리키는 씬마다 반드시 동일한 값을 써야 한다. 예: \"Player\", \"UICanvas\"")]
        [SerializeField] private string singletonId = "";

        // DisableDomainReload 프로젝트: 이 static은 에디터 플레이 세션 간 생존한다(FAIL.md #H3/#28
        // 규칙). 새 플레이 시작 시 반드시 비워야 이전 세션의 죽은 참조가 남지 않는다.
        private static readonly Dictionary<string, PersistentSingleton> instances = new Dictionary<string, PersistentSingleton>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instances.Clear();
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(singletonId))
            {
                Debug.LogWarning("[PersistentSingleton] singletonId가 비어있습니다: " + gameObject.name, this);
                return;
            }

            if (instances.TryGetValue(singletonId, out PersistentSingleton existing) && existing != null && existing != this)
            {
                // 이미 DontDestroyOnLoad로 살아남은 원본이 있다 — 이번에 새로 로드된 씬의
                // 중복 인스턴스는 파괴한다.
                Destroy(gameObject);
                return;
            }

            instances[singletonId] = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instances.TryGetValue(singletonId, out PersistentSingleton existing) && existing == this)
                instances.Remove(singletonId);
        }
    }
}
