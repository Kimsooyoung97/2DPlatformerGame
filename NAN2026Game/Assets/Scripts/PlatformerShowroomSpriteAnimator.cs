using UnityEngine;

namespace NAN2026.Showroom
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlatformerShowroomSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8f;

        private SpriteRenderer spriteRenderer;

        public void Configure(Sprite[] animationFrames, float speed)
        {
            frames = animationFrames;
            framesPerSecond = Mathf.Max(0.1f, speed);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
                return;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            int frameIndex = Mathf.FloorToInt(Time.unscaledTime * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
