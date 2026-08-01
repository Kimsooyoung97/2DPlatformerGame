using UnityEngine;

namespace NAN2026
{
    public class EffectProjectile : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate;
        [SerializeField] private float speed;
        [SerializeField] private float lifetime;

        private SpriteRenderer sr;
        private float age;
        private float dir = 1f;

        public void Launch(float direction, float moveSpeed, float life, Sprite[] animFrames, float fps)
        {
            dir = direction;
            speed = moveSpeed;
            lifetime = life;
            frames = animFrames;
            frameRate = fps;
            var s = GetComponent<SpriteRenderer>();
            s.flipX = direction < 0f;
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime) { Destroy(gameObject); return; }
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
            if (frames != null && frames.Length > 0 && frameRate > 0f)
            {
                int idx = (int)(age * frameRate) % frames.Length;
                sr.sprite = frames[idx];
            }
        }
    }
}