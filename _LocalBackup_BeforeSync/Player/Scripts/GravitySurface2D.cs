using UnityEngine;

namespace NHNDemo
{
    [DisallowMultipleComponent]
    public sealed class GravitySurface2D : MonoBehaviour
    {
        [SerializeField] private bool allowsGravityShift = true;
        [SerializeField] private Vector2 surfaceUp = Vector2.up;

        public bool AllowsGravityShift => allowsGravityShift;
        public Vector2 SurfaceUp => surfaceUp.normalized;

        public void Configure(Vector2 inwardNormal)
        {
            surfaceUp = inwardNormal.normalized;
        }
    }
}
