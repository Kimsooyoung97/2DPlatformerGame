using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Game/CameraConfig")]
    public class CameraConfig : ScriptableObject
    {
        public float smoothTime = 0.15f;
        public Vector2 offset = new Vector2(0f, 1f);
    }
}