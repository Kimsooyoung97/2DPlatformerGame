using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    [RequireComponent(typeof(Animator))]
    public class BossIntroSequencer : MonoBehaviour
    {
        [SerializeField] private BossConfig config;

        private Animator anim;
        private bool triggered;
        private float elapsed;
        private int lastStage = -1;
        private float[] stageDurations;
        private static readonly string[] StageStates = { "PTrans1", "PTrans2", "PTrans3" };

        private void Awake()
        {
            anim = GetComponent<Animator>();
            var clips = anim.runtimeAnimatorController.animationClips;
            float t1 = 0f, t2 = 0f, t3 = 0f;
            foreach (var c in clips)
            {
                if (c.name == "Princess_Trans1") t1 = c.length;
                else if (c.name == "Princess_Trans2") t2 = c.length;
                else if (c.name == "Princess_Trans3") t3 = c.length;
            }
            stageDurations = new float[] { t1, t2, t3 };
        }

        private void Start()
        {
            anim.Play("PIdle1", 0, 0f);
        }

        private void Update()
        {
            if (!triggered)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                {
                    triggered = true;
                    elapsed = 0f;
                    lastStage = -1;
                }
                return;
            }
            elapsed += Time.deltaTime;
            int stage = PlayerLocomotionLogic.SequenceStage(elapsed, stageDurations);
            if (stage == lastStage) return;
            lastStage = stage;
            if (stage < StageStates.Length) anim.Play(StageStates[stage], 0, 0f);
            else
            {
                anim.Play("PIdle2", 0, 0f);
                var launcher = GetComponent<BossOrbLauncher>();
                if (launcher != null) launcher.enabled = true;
                enabled = false;
            }
        }
    }
}