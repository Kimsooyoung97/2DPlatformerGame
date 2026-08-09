using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    /// 원거리 잡몹. row1 활쏘기 도중 화살을 발사한다.
    /// GandalfHardcore 시트는 비반전 상태에서 오른쪽을 향한다.
    public class ArcherEnemy : EnemyBase
    {
        public Sprite arrowSprite;
        private bool firedThisDraw;

        protected override bool FlipFor(float face) { return face < 0f; }

        protected override void SetState(int s) { base.SetState(s); firedThisDraw = false; }

        protected override void DoAttack(float dx, float face)
        {
            Anim(attackFrames, false, SwingFps);
            float frac = stateT / config.attackDur;
            if (EnemyStateLogic.ShouldFire(frac, config.fireFrac, firedThisDraw))
            {
                firedThisDraw = true;
                Fire(face);
            }
            if (frac >= 1f) { nextAtk = NextAttackAt(); SetState(EnemyStateLogic.Idle); }
        }

        private void Fire(float face)
        {
            if (arrowSprite == null) return;
            var go = new GameObject("ArcherArrow");
            go.transform.position = transform.position
                + new Vector3(config.muzzleOffset.x * face, config.muzzleOffset.y, 0f);
            var a = go.AddComponent<ArcherArrow>();
            a.Launch(arrowSprite, new Vector2(face, 0f), config.arrowSpeed, config.arrowLife, config.arrowDamage,
                     sr != null ? sr.sortingOrder : 0, config.clashConfig);
        }
    }
}
