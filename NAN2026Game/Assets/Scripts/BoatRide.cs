using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 배: 갑판을 밟으면 물 끝(오른쪽)까지 항해. 탑승자는 갑판 이동량만큼 같이 운반.
    public class BoatRide : MonoBehaviour
    {
        public BoatRideConfig config;
        private Transform player;
        private Tilemap water;
        private float targetX;
        private bool sailing, arrived;

        private void Start()
        {
            var p = GameObject.Find("Player");
            if (p != null) player = p.transform;
            var w = GameObject.Find("Stage_Wall");
            if (w != null) water = w.GetComponent<Tilemap>();
            targetX = ComputeWaterEndX();
        }

        private float ComputeWaterEndX()
        {
            if (water == null || config == null) return transform.position.x;
            var c0 = water.WorldToCell(transform.position);
            int rowY = c0.y;
            bool found = false;
            for (int dy = 1; dy >= -2 && !found; dy--)
                if (water.GetTile(new Vector3Int(c0.x, c0.y + dy, 0)) != null) { rowY = c0.y + dy; found = true; }
            if (!found) return transform.position.x;
            int x = c0.x;
            while (water.GetTile(new Vector3Int(x + 1, rowY, 0)) != null) x++;
            return water.CellToWorld(new Vector3Int(x, rowY, 0)).x + 1f - config.deckHalfWidth - config.edgeMargin;
        }

        private bool RiderOnDeck()
        {
            if (player == null || config == null) return false;
            Vector3 d = player.position - transform.position;
            return Mathf.Abs(d.x) <= config.deckHalfWidth
                && d.y >= config.deckTopOffset - 0.4f
                && d.y <= config.deckTopOffset + config.riderGrace;
        }

        private void FixedUpdate()
        {
            if (config == null || arrived && !sailing) { }
            bool rider = RiderOnDeck();
            if (!sailing && !arrived && rider) sailing = true;
            if (!sailing) return;
            float nx = Mathf.MoveTowards(transform.position.x, targetX, config.sailSpeed * Time.fixedDeltaTime);
            float dx = nx - transform.position.x;
            transform.position = new Vector3(nx, transform.position.y, transform.position.z);
            if (rider && dx != 0f) player.position += new Vector3(dx, 0f, 0f);
            if (Mathf.Approximately(nx, targetX)) { sailing = false; arrived = true; }
        }
    }
}
