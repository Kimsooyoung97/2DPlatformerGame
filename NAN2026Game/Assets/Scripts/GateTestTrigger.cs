using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // [임시 테스트] 시작 시 몬스터 AI 정지 + 우클릭으로 게이트 붕괴 재생.
    // 제거: GateDirector에서 이 컴포넌트 삭제만 하면 된다.
    public class GateTestTrigger : MonoBehaviour
    {
        public GateCollapseSequencer sequencer;
        static readonly string[] FreezeTypes = { "MonsterController2D", "EnemyAI", "MonsterControls" };

        void Start()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                for (int i = 0; i < FreezeTypes.Length; i++)
                    if (n == FreezeTypes[i])
                    {
                        mb.enabled = false;
                        var rb = mb.GetComponent<Rigidbody2D>();
                        if (rb != null) rb.linearVelocity = Vector2.zero;
                    }
            }
        }

        void Update()
        {
            if (sequencer != null && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                sequencer.Play();
        }
    }
}
