using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 물 입수 사망: 몸 중심이 물 타일 칸에 잠기면 시작 지점으로 리스폰
    public class WaterDeath : MonoBehaviour
    {
        private Vector3 spawn;
        private Tilemap water;
        private Rigidbody2D rb;

        private void Start()
        {
            spawn = transform.position;
            rb = GetComponent<Rigidbody2D>();
            var w = GameObject.Find("Stage_Wall");
            if (w != null) water = w.GetComponent<Tilemap>();
        }

        private void FixedUpdate()
        {
            if (water == null) return;
            var t = water.GetTile(water.WorldToCell(transform.position));
            if (t == null || !t.name.StartsWith("Water")) return;
            if (t.name == "WaterTiles_4_1") return; // 밟는 물은 예외
            transform.position = spawn;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}
