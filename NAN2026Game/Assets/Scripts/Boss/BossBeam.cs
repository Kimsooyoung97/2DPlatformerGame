using UnityEngine;

namespace NAN2026
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossBeam : MonoBehaviour
    {
        private BossConfig config;
        private GameObject notePrefab;
        private Transform target;
        private Vector3 origin;
        private float dir;
        private float elapsed;
        private int nextNote;
        private float endTime;

        public void Init(BossConfig cfg, GameObject notePf, Vector3 beamOrigin, Transform beamTarget)
        {
            config = cfg;
            notePrefab = notePf;
            target = beamTarget;
            origin = beamOrigin;
            dir = beamTarget.position.x > beamOrigin.x ? 1f : -1f;
            float len = Mathf.Abs(beamTarget.position.x - beamOrigin.x) + config.beamOverreach;
            var sr = GetComponent<SpriteRenderer>();
            sr.color = config.beamColor;
            transform.position = new Vector3(beamOrigin.x + dir * len * 0.5f, beamOrigin.y, 0f);
            transform.localScale = new Vector3(len, config.beamThickness, 1f);
            float last = 0f;
            foreach (float t in config.notePattern) if (t > last) last = t;
            endTime = config.beamLeadIn + last + config.beamTailTime;
        }

        public bool IsDone { get { return elapsed >= endTime; } }

        private void Update()
        {
            if (config == null) return;
            elapsed += Time.deltaTime;
            while (nextNote < config.notePattern.Length && elapsed >= config.beamLeadIn + config.notePattern[nextNote])
            {
                var go = Instantiate(notePrefab, new Vector3(origin.x, origin.y, 0f), Quaternion.identity);
                go.transform.localScale = new Vector3(config.noteScale, config.noteScale, 1f);
                var bn = go.GetComponent<BeamNote>();
                if (bn != null)
                {
                    bn.Launch(dir, config.beamNoteSpeed, config.orbLifetime);
                    bn.SetMissRule(target, config.missBehindDistance);
                }
                nextNote++;
            }
            if (elapsed >= endTime) Destroy(gameObject);
        }
    }
}