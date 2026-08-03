using UnityEngine;

namespace NAN2026
{
    // 투명 충돌 박스: 게임에선 안 보이고 씬 뷰에선 초록 박스로 표시
    [RequireComponent(typeof(BoxCollider2D))]
    public class InvisiblePlatform : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}
