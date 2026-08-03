using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using NAN2026.Core;

namespace NAN2026
{
    // 게이트 붕괴: 지연 -> 틴트 소거+먼지+파편 -> 개방부 점화 -> 카메라 복귀
    public class GateCollapseSequencer : MonoBehaviour
    {
        public GateConfig config;
        public Tilemap lockedTilemap;
        public GameObject lockedRoot;
        public CinemachineCamera vcam;
        public Transform panAnchor;
        public Light2D openLight;
        public ParticleSystem dustTemplate;
        public GameObject[] debrisPrefabs;
        public Vector3[] dustPoints;
        public SpriteRenderer[] wallSprites;
        public CinemachineBasicMultiChannelPerlin noise;
        public Light2D[] barrierLights;
        public ParticleSystem sparkTemplate;

        float t;
        float lockedBaseAlpha = 1f;
        bool playing;
        bool collapseFired;
        Transform origTarget;

        public void Play()
        {
            if (playing) return;
            playing = true;
            t = 0f;
            collapseFired = false;
            if (vcam != null && panAnchor != null)
            {
                origTarget = vcam.Target.TrackingTarget;
                vcam.Target.TrackingTarget = panAnchor;
            }
            enabled = true;
        }

        void Awake()
        {
            enabled = false;
            if (openLight != null) openLight.intensity = 0f;
            if (lockedTilemap != null) lockedBaseAlpha = lockedTilemap.color.a;
        }

        void Update()
        {
            if (!playing) return;
            t += Time.deltaTime;
            float d = config.delaySeconds, c = config.collapseSeconds, h = config.holdSeconds;

            if (noise != null)
                noise.AmplitudeGain = GateCollapseLogic.GetPhase(t, d, c, h) == 1 ? config.shakeAmplitude : 0f;

            if (wallSprites != null)
            {
                float wa = GateCollapseLogic.TintAlpha(t, d, c);
                for (int i = 0; i < wallSprites.Length; i++)
                    if (wallSprites[i] != null)
                    { var wc = wallSprites[i].color; wc.a = wa; wallSprites[i].color = wc; }
            }

            if (lockedTilemap != null)
            {
                var col = lockedTilemap.color;
                col.a = lockedBaseAlpha * GateCollapseLogic.TintAlpha(t, d, c);
                lockedTilemap.color = col;
            }

            if (barrierLights != null)
            {
                float bf = GateCollapseLogic.TintAlpha(t, d, c);
                for (int i = 0; i < barrierLights.Length; i++)
                    if (barrierLights[i] != null) barrierLights[i].intensity = config.barrierLightIntensity * bf;
            }

            int phase = GateCollapseLogic.GetPhase(t, d, c, h);
            if (phase >= 1 && !collapseFired) { collapseFired = true; FireCollapse(); }
            if (phase >= 2 && lockedRoot != null && lockedRoot.activeSelf) lockedRoot.SetActive(false);

            if (openLight != null)
                openLight.intensity = config.lightIntensity * GateCollapseLogic.LightFactor(t, d, c, h);

            if (!GateCollapseLogic.PanActive(t, d, c, h) && vcam != null && origTarget != null)
            {
                vcam.Target.TrackingTarget = origTarget;
                origTarget = null;
            }

            if (t > d + c + h + 1.5f) { playing = false; enabled = false; }
        }

        void FireCollapse()
        {
            // 충돌 즉시 해제 (시각 소거보다 먼저 길이 열려도 무방)
            if (lockedRoot != null)
                foreach (var cl in lockedRoot.GetComponentsInChildren<Collider2D>())
                    cl.enabled = false;

            // 돌무더기 실물 낙하: 각 벽돌에 물리 부여 + 수명 후 정리
            if (wallSprites != null)
                for (int i = 0; i < wallSprites.Length; i++)
                {
                    if (wallSprites[i] == null) continue;
                    var bgo = wallSprites[i].gameObject;
                    var brb = bgo.GetComponent<Rigidbody2D>();
                    if (brb == null) brb = bgo.AddComponent<Rigidbody2D>();
                    brb.gravityScale = 1.6f;
                    brb.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 1f)) * config.debrisImpulse, ForceMode2D.Impulse);
                    brb.AddTorque(Random.Range(-3f, 3f), ForceMode2D.Impulse);
                    Destroy(bgo, config.debrisLifetime);
                }

            if (dustTemplate != null && dustPoints != null)
                foreach (var p in dustPoints)
                {
                    var ps = Instantiate(dustTemplate, p, Quaternion.identity);
                    ps.gameObject.SetActive(true);
                    ps.Play();
                    Destroy(ps.gameObject, config.dustLifetime);
                }

            if (sparkTemplate != null && barrierLights != null)
                foreach (var bl in barrierLights)
                    if (bl != null)
                    {
                        var sk = Instantiate(sparkTemplate, bl.transform.position, Quaternion.identity);
                        sk.gameObject.SetActive(true);
                        sk.Play();
                        Destroy(sk.gameObject, config.dustLifetime);
                    }

            if (debrisPrefabs != null && debrisPrefabs.Length > 0 && panAnchor != null)
                for (int i = 0; i < config.debrisCount; i++)
                {
                    var pf = debrisPrefabs[i % debrisPrefabs.Length];
                    if (pf == null) continue;
                    var basePos = (dustPoints != null && dustPoints.Length > 0) ? dustPoints[i % dustPoints.Length] : panAnchor.position;
                    var off = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.6f), 0f);
                    var go = Instantiate(pf, basePos + off, Quaternion.identity);
                    var rb = go.GetComponent<Rigidbody2D>();
                    if (rb == null) rb = go.AddComponent<Rigidbody2D>();
                    rb.AddForce(new Vector2(Random.Range(-1f, 1f), 1f) * config.debrisImpulse, ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
                    Destroy(go, config.debrisLifetime);
                }
        }
    }
}
