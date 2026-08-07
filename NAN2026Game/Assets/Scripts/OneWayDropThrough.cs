using UnityEngine;
using System.Collections.Generic;

namespace NAN2026
{
    // 메이플식 하향 점프: 발판 레이어에 부착. 발판 위의 플레이어가 ↓+점프 시
    // 이 오브젝트의 엣지들을 잠깐 꺼서 아래로 통과시킨다. 팀 코드 무수정.
    public class OneWayDropThrough : MonoBehaviour
    {
        [Tooltip("통과 허용 시간(초)")]
        public float dropDuration = 0.3f;

        Collider2D[] edges;
        Collider2D playerCol;
        Rigidbody2D playerRb;
        float reenableAt = -1f;

        void Start()
        {
            edges = GetComponents<Collider2D>();
            var p = GameObject.Find("Player");
            if (p != null)
            {
                playerCol = p.GetComponent<Collider2D>();
                playerRb = p.GetComponent<Rigidbody2D>();
            }
        }

        void Update()
        {
            if (playerCol == null) return;
            if (reenableAt > 0f)
            {
                if (Time.time >= reenableAt)
                {
                    foreach (var e in edges) if (e != null) Physics2D.IgnoreCollision(e, playerCol, false);
                    reenableAt = -1f;
                }
                return;
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            bool jump = kb.spaceKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame;
            if (!(down && jump)) return;
            // 발판 위에 서 있을 때만 (플레이어 발이 엣지 근처)
            bool onThis = false;
            float footY = playerCol.bounds.min.y;
            float px = playerCol.bounds.center.x;
            foreach (var e in edges)
            {
                if (e == null) continue;
                var b = e.bounds;
                if (px >= b.min.x - 0.1f && px <= b.max.x + 0.1f && Mathf.Abs(footY - b.max.y) < 0.25f)
                { onThis = true; break; }
            }
            if (!onThis) return;
            foreach (var e in edges) if (e != null) Physics2D.IgnoreCollision(e, playerCol, true);
            if (playerRb != null && playerRb.linearVelocity.y > 0f)
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
            reenableAt = Time.time + dropDuration;
        }
    }
}
