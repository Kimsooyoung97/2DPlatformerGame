using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 입수 연출: 수면에서 살짝 떠올랐다가 가라앉아 물 뒤로 사라짐.
    // 완전히 잠기면 PlayerHealth.Kill() 로 기존 사망·체크포인트 흐름에 합류한다.
    public class WaterDeath : MonoBehaviour
    {
        public WaterSinkConfig config;
        private Vector3 spawn;
        private Tilemap water;
        private Tilemap[] waters;   // 물 타일이 여러 층에 깔려 있어 전부 검사한다
        private Rigidbody2D rb;
        private Behaviour controller;
        private int state; // 0대기 1내밈 2침강 3잠김 4사망대기
        private float timer;
        private float surfaceY, sinkFromY;

        private void Start()
        {
            spawn = transform.position;
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent("PlayerController2D") as Behaviour;
            var list = new System.Collections.Generic.List<Tilemap>();
            foreach (var nm in new[] { "Stage_Wall", "Stage_Ground", "Stage_Solid", "Stage_Water" })
            {
                var g = GameObject.Find(nm);
                if (g == null) continue;
                var tm = g.GetComponent<Tilemap>();
                if (tm != null) list.Add(tm);
            }
            waters = list.ToArray();
            water = waters.Length > 0 ? waters[0] : null;
        }

        private bool InWaterCell(Vector3 pos)
        {
            if (waters == null) return false;
            for (int i = 0; i < waters.Length; i++)
            {
                var tm = waters[i];
                if (tm == null) continue;
                var t = tm.GetTile(tm.WorldToCell(pos));
                if (t == null || !t.name.StartsWith("Water") || t.name == "WaterTiles_4_1") continue;
                water = tm;   // 수면 계산은 실제로 물이 있던 층 기준
                return true;
            }
            return false;
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
            var hp0 = GetComponent<PlayerHealth>();
            if (hp0 != null) hp0.SinkingInWater = true;   // 낙사 판정이 수몰 연출을 가로채지 않도록
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
                timer += Time.fixedDeltaTime;
                if (timer < config.respawnDelay) return;

                // 물리·조작을 먼저 되살린 뒤 게임의 정상 사망 경로에 넘긴다
                if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
                if (controller != null) controller.enabled = true;

                var hp = GetComponent<PlayerHealth>();
                if (config.useDeathFlow && hp != null)
                {
                    if (hp != null) hp.SinkingInWater = false;
            hp.Kill();          // 체크포인트 부활이냐 게임오버냐는 PlayerHealth 가 판단한다
                    state = 4; timer = 0f;
                    return;
                }

                transform.position = spawn;   // PlayerHealth 가 없을 때만 쓰는 대비책
                state = 0; timer = 0f;
            }
            else if (state == 4)
            {
                // 부활로 물 밖에 나갈 때까지 대기 — 같은 자리에서 즉시 재발동하는 것을 막는다
                if (!InWaterCell(transform.position))
            {
                state = 0; timer = 0f;
                var hp1 = GetComponent<PlayerHealth>();
                if (hp1 != null) hp1.SinkingInWater = false;
            }
            }
        }
    }
}