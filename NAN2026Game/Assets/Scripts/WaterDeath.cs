using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 입수 연출: 수면에서 살짝 떠올랐다가 가라앉아 물 뒤로 사라짐.
    // 이후 처리(숨막힘 장면)는 미구현 — respawnDelay는 테스트용 임시 자리표시.
    public class WaterDeath : MonoBehaviour
    {
        public WaterSinkConfig config;
        private Vector3 spawn;
        private Tilemap water;
        private Rigidbody2D rb;
        private Behaviour controller;
        private int state; // 0대기 1내밈 2침강 3잠김
        private float timer;
        private float surfaceY, sinkFromY;

        private void Start()
        {
            spawn = transform.position;
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent("PlayerController2D") as Behaviour;
            var w = GameObject.Find("Stage_Wall");
            if (w != null) water = w.GetComponent<Tilemap>();
        }

        private bool InWaterCell(Vector3 pos)
        {
            if (water == null) return false;
            var t = water.GetTile(water.WorldToCell(pos));
            return t != null && t.name.StartsWith("Water") && t.name != "WaterTiles_4_1";
        }

        private void FixedUpdate()
        {
            if (config == null) return;
            if (state == 0)
            {
                if (!InWaterCell(transform.position)) return;
                // 수면 탐색: 이 열에서 물이 끝나는 위 칸
                var c = water.WorldToCell(transform.position);
                int top = c.y;
                while (water.GetTile(new Vector3Int(c.x, top + 1, 0)) != null && water.GetTile(new Vector3Int(c.x, top + 1, 0)).name.StartsWith("Water")) top++;
                surfaceY = water.CellToWorld(new Vector3Int(c.x, top, 0)).y + 1f;
                state = 1; timer = 0f;
                if (controller != null) controller.enabled = false;
                if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
                transform.position = new Vector3(transform.position.x, surfaceY - 0.5f + config.peekHeight, transform.position.z);
                sinkFromY = transform.position.y;
            }
            else if (state == 1)
            {
                timer += Time.fixedDeltaTime;
                if (timer >= config.peekTime) { state = 2; timer = 0f; }
            }
            else if (state == 2)
            {
                timer += Time.fixedDeltaTime;
                float k = Mathf.Clamp01(timer / config.sinkTime);
                float y = Mathf.Lerp(sinkFromY, surfaceY - config.sinkDepth, k * k); // 점점 빨라지는 침강
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                if (k >= 1f) { state = 3; timer = 0f; }
            }
            else if (state == 3)
            {
                if (config.respawnDelay <= 0f) return; // 이후 처리는 추후 장면에서
                timer += Time.fixedDeltaTime;
                if (timer < config.respawnDelay) return;
                transform.position = spawn;
                if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
                if (controller != null) controller.enabled = true;
                state = 0; timer = 0f;
            }
        }
    }
}
