using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Watches the character controller's animation state and throws a slash wave
    /// whenever a sword attack starts.
    ///
    ///   BASIC   - SwordAttack (K) and the first three combo hits (J)
    ///   POWERED - the combo finisher and the dash slash (H), so the last hit reads heavier
    ///
    /// It reads the controller's public CurrentAnimation rather than duplicating the input
    /// rules, so the effect always fires exactly in step with the animation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwordSlashSpawner : MonoBehaviour
    {
        [Header("Frames (assign from the sliced sheet)")]
        [SerializeField] private Sprite[] basicFrames;
        [SerializeField] private Sprite[] poweredFrames;

        [Header("Basic")]
        [SerializeField] private float basicSpeed = 11f;
        [SerializeField] private int basicDamage = 1;
        [SerializeField] private float basicScale = 1f;

        [Header("Powered")]
        [SerializeField] private float poweredSpeed = 13f;
        [SerializeField] private int poweredDamage = 3;
        [SerializeField] private float poweredScale = 1.35f;

        [Header("Common")]
        [SerializeField] private float framesPerSecond = 14f;
        [SerializeField] private float forwardOffset = 0.85f;
        [SerializeField] private float heightOffset = 0.8f;
        [SerializeField] private int sortingOrder = 13;
        [SerializeField] private float hitboxSize = 0.8f;

        private static readonly string[] BasicStates =
        {
            "SwordAttack", "ComboAttackA", "ComboAttackB", "ComboAttackC"
        };

        private static readonly string[] PoweredStates =
        {
            "ComboAttackD", "SwordSprintSlash"
        };

        private MonoBehaviour controller;
        private System.Reflection.PropertyInfo animationProperty;
        private SpriteRenderer visual;
        private string lastState = string.Empty;

        public void Configure(Sprite[] basic, Sprite[] powered)
        {
            basicFrames = basic;
            poweredFrames = powered;
        }

        private void Awake()
        {
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == this || behaviour.GetType().Name != "PixelPlayerController")
                    continue;

                controller = behaviour;
                animationProperty = behaviour.GetType().GetProperty("CurrentAnimation");
                break;
            }

            visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (controller == null || animationProperty == null)
                return;

            string state = animationProperty.GetValue(controller) as string;
            if (string.IsNullOrEmpty(state) || state == lastState)
                return;

            lastState = state;

            if (System.Array.IndexOf(PoweredStates, state) >= 0)
                Throw(poweredFrames, poweredSpeed, poweredDamage, poweredScale);
            else if (System.Array.IndexOf(BasicStates, state) >= 0)
                Throw(basicFrames, basicSpeed, basicDamage, basicScale);
        }

        private void Throw(Sprite[] frames, float speed, int damage, float scale)
        {
            if (frames == null || frames.Length == 0)
                return;

            float facing = visual != null && visual.flipX ? -1f : 1f;

            GameObject slash = new GameObject("SlashWave");
            slash.transform.position = transform.position +
                                       Vector3.right * (facing * forwardOffset) +
                                       Vector3.up * heightOffset;
            slash.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = sortingOrder;

            BoxCollider2D collider = slash.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one * hitboxSize;

            SlashProjectile projectile = slash.AddComponent<SlashProjectile>();
            projectile.Launch(frames, facing, speed, damage, framesPerSecond);
        }
    }
}
