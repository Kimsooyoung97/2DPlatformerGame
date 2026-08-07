using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026
{
    // 출렁이는 물 타일 — 프레임 배열을 타일맵이 직접 재생 (패키지 불요)
    [CreateAssetMenu(fileName = "AnimWaterTile", menuName = "NAN2026/AnimWaterTile")]
    public class AnimWaterTile : TileBase
    {
        public Sprite[] frames;
        public float speed = 6f;
        public Tile.ColliderType collider = Tile.ColliderType.None;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            tileData.colliderType = collider;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (frames == null || frames.Length < 2) return false;
            tileAnimationData.animatedSprites = frames;
            tileAnimationData.animationSpeed = speed;
            tileAnimationData.animationStartTime = 0f;
            return true;
        }
    }
}
