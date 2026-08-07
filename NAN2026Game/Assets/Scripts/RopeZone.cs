using UnityEngine;

namespace NAN2026
{
    // 밧줄 등반 구역 마커 — BoxCollider2D(트리거)와 함께 사용
    [RequireComponent(typeof(BoxCollider2D))]
    public class RopeZone : MonoBehaviour
    {
        private void Reset()
        {
            var c = GetComponent<BoxCollider2D>();
            c.isTrigger = true;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                c.size = sr.sprite.bounds.size;
                c.offset = sr.sprite.bounds.center;
            }
        }
    }
}
