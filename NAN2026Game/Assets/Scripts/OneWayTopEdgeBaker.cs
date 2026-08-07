using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace NAN2026
{
    // 메이플식 발판선: 타일맵의 '노출된 윗면'만 EdgeCollider2D 라인으로 굽는다.
    // 옆·아래·내부는 콜라이더 자체가 없다. Awake마다 재베이크 — 페인팅 즉시 반영.
    [RequireComponent(typeof(Tilemap))]
    public class OneWayTopEdgeBaker : MonoBehaviour
    {
        void Awake() { Bake(); }

        [ContextMenu("Bake Top Edges")]
        public void Bake()
        {
            var tm = GetComponent<Tilemap>();
            // 기존 엣지 전부 제거
            foreach (var ec in GetComponents<EdgeCollider2D>())
            {
                if (Application.isPlaying) Destroy(ec); else DestroyImmediate(ec);
            }
            tm.CompressBounds();
            var b = tm.cellBounds;
            // 행별로 '윗면 노출' 런을 찾아 선분 생성
            for (int y = b.yMin; y < b.yMax; y++)
            {
                int runStart = int.MinValue;
                for (int x = b.xMin; x <= b.xMax; x++)
                {
                    bool top = x < b.xMax
                        && tm.GetTile(new Vector3Int(x, y, 0)) != null
                        && tm.GetTile(new Vector3Int(x, y + 1, 0)) == null;
                    if (top && runStart == int.MinValue) runStart = x;
                    if (!top && runStart != int.MinValue)
                    {
                        var ec = gameObject.AddComponent<EdgeCollider2D>();
                        ec.usedByEffector = true;
                        ec.points = new Vector2[]
                        {
                            new Vector2(runStart, y + 1),
                            new Vector2(x, y + 1)
                        };
                        runStart = int.MinValue;
                    }
                }
            }
        }
    }
}
