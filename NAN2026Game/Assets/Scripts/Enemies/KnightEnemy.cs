using UnityEngine;

namespace NAN2026
{
    /// 근접 잡몹. ATTACK3 로 후려치고, 피격 시 HURT, 5대 맞으면 DEATH.
    /// Knight 2D 시트는 비반전 상태에서 오른쪽을 향한다.
    public class KnightEnemy : EnemyBase
    {
        protected override bool FlipFor(float face) { return face < 0f; }
    }
}
