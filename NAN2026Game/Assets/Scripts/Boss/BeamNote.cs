using UnityEngine;

namespace NAN2026
{
    public class BeamNote : BossOrb
    {
        private Transform missTarget;
        private float missBehind;
        private bool missReported;

        public void SetMissRule(Transform target, float behindDistance)
        {
            missTarget = target;
            missBehind = behindDistance;
        }

        protected override void Tick()
        {
            base.Tick();
            if (missReported || missTarget == null) return;
            bool passed = dir < 0f
                ? transform.position.x < missTarget.position.x - missBehind
                : transform.position.x > missTarget.position.x + missBehind;
            if (passed)
            {
                missReported = true;
                FloatingText.Spawn(missTarget.position + Vector3.up * 1.1f, "miss", Color.red);
                Destroy(gameObject);
            }
        }
    }
}