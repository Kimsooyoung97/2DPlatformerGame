using UnityEngine;

namespace NAN2026
{
    public class BossOrbLauncher : MonoBehaviour
    {
        [SerializeField] private BossConfig config;
        [SerializeField] private GameObject orbPrefab;
        [SerializeField] private GameObject notePrefab;
        [SerializeField] private GameObject beamPrefab;
        [SerializeField] private Transform target;

        private float timer;
        private int orbsFired;
        private BossBeam activeBeam;

        private void OnEnable()
        {
            timer = config != null ? config.orbInterval : 0f;
            orbsFired = 0;
        }

        private void Update()
        {
            if (config == null) return;
            if (target == null)
            {
                var pc = FindFirstObjectByType<PlayerController2D>();
                if (pc != null) target = pc.transform;
                else return;
            }
            if (activeBeam != null) return; // 빔 진행 중 (파괴되면 null)
            if (orbsFired >= config.orbsPerCycle)
            {
                orbsFired = 0;
                timer = config.orbInterval;
                var beamGo = Instantiate(beamPrefab);
                activeBeam = beamGo.GetComponent<BossBeam>();
                Vector3 orig = transform.position + new Vector3(0f, config.orbSpawnHeight, 0f);
                Vector3 beamOrigin = new Vector3(orig.x, target.position.y + config.beamHeightOffset, 0f);
                activeBeam.Init(config, notePrefab, beamOrigin, target);
                return;
            }
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.orbInterval;
            Vector3 pos = transform.position + new Vector3(0f, config.orbSpawnHeight, 0f);
            var go = Instantiate(orbPrefab, pos, Quaternion.identity);
            var ob = go.GetComponent<BossOrb>();
            if (ob != null) ob.LaunchAt(target.position + Vector3.up * config.orbAimHeight, config.orbSpeed, config.orbLifetime);
            orbsFired++;
        }
    }
}