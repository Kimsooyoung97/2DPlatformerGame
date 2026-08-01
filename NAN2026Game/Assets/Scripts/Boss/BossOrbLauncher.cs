using UnityEngine;

namespace NAN2026
{
    public class BossOrbLauncher : MonoBehaviour
    {
        [SerializeField] private BossConfig config;
        [SerializeField] private GameObject orbPrefab;
        [SerializeField] private Transform target;

        private float timer;

        private void OnEnable()
        {
            timer = config != null ? config.orbInterval : 0f;
        }

        private void Update()
        {
            if (config == null || orbPrefab == null) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.orbInterval;
            float dir = target != null && target.position.x > transform.position.x ? 1f : -1f;
            Vector3 pos = transform.position + new Vector3(0f, config.orbSpawnHeight, 0f);
            var go = Instantiate(orbPrefab, pos, Quaternion.identity);
            var ob = go.GetComponent<BossOrb>();
            if (ob != null) ob.Launch(dir, config.orbSpeed, config.orbLifetime);
        }
    }
}