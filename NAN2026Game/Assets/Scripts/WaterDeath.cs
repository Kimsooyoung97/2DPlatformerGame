using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 입수 연출: 수면에서 살짝 떠올랐다가 가라앉아 물 뒤로 사라짐.
    // 완전히 잠기면 PlayerHealth.Kill() 로 기존 사망·체크포인트 흐름에 합류한다.
    // FAIL: 예전엔 Kill() 호출 이후에도 이 스크립트가 계속 FixedUpdate를 돌려서, Respawn()이
    // checkpoint로 세팅한 Y를 물 재진입 로직(peek 위치 강제 대입)이 다시 덮어쓰는 버그가 있었다.
    // health.OnPlayerDied 시점에 컴포넌트를 통째로 비활성화해 원천 차단한다.
    public class WaterDeath : MonoBehaviour
    {
        public WaterSinkConfig config;
        private Vector3 spawn;
        private Tilemap water;
        private Tilemap[] waters;   // 물 타일이 여러 층에 깔려 있어 전부 검사한다
        private Rigidbody2D rb;
        private Behaviour controller;
        private PlayerHealth health;
        private int state; // 0대기 1내밈 2침강 3잠김 4사망대기
        private float timer;
        private float surfaceY, sinkFromY;

        private void Start()
        {
            spawn = transform.position;
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent("PlayerController2D") as Behaviour;
            health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.OnPlayerDied += HandleDied;
                health.OnPlayerRespawned += HandleRespawned;
            }
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

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnPlayerDied -= HandleDied;
                health.OnPlayerRespawned -= HandleRespawned;
            }
        }

        // Kill()이 확정되는 순간(Respawn() 예약 직전) 동기로 호출된다. 이 프레임 이후로는
        // FixedUpdate 자체가 안 돌아서 transform.position을 건드릴 방법이 없어진다.
        private void HandleDied()
        {
            enabled = false;
        }

        // Respawn()이 checkpoint로 위치를 다 세팅한 뒤 호출된다. 상태를 대기로 되돌리고
        // 다시 켠다 — 물 밖에서 부활했다면 다음 FixedUpdate의 InWaterCell 체크가 자연히 false.
        private void HandleRespawned()
        {
            state = 0; timer = 0f;
            if (health != null) health.SinkingInWater = false; // 사망 경로가 다른 경우(낙사 등) 대비 방어적 리셋
            enabled = true;
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
                if (health != null) health.SinkingInWater = true;   // 낙사 판정이 수몰 연출을 가로채지 않도록
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

                if (config.useDeathFlow && health != null)
                {
                    health.SinkingInWater = false;
                    state = 4; timer = 0f;
                    health.Kill();   // 체크포인트 부활이냐 게임오버냐는 PlayerHealth 가 판단한다.
                                     // 이 호출이 동기적으로 HandleDied()를 발화시켜 이 컴포넌트를 즉시 비활성화한다 —
                                     // 아래로 더 이상 실행될 코드가 없으므로 state=4 관련 로직은 무의미하지만
                                     // 안전하게 남겨둔다(다음 FixedUpdate가 애초에 안 불림).
                    return;
                }

                transform.position = spawn;   // PlayerHealth 가 없을 때만 쓰는 대비책
                state = 0; timer = 0f;
            }
            else if (state == 4)
            {
                // enabled=false로 꺼지기 때문에 정상 흐름에서는 이 분기에 도달하지 않는다.
                // useDeathFlow가 켜졌다가 런타임에 꺼지는 등 예외적인 경우를 대비한 방어 코드.
                if (!InWaterCell(transform.position))
                {
                    state = 0; timer = 0f;
                    if (health != null) health.SinkingInWater = false;
                }
            }
        }
    }
}