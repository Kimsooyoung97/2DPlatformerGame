using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Marker for anything that kills the player on touch.
    /// PlayerHealth looks for this in the parent chain of a trigger it overlaps.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Hazard2D : MonoBehaviour
    {
    }
}
