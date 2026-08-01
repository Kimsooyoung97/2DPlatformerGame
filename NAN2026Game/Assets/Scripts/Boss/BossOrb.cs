using UnityEngine;

namespace NAN2026
{
    public class BossOrb : MonoBehaviour
    {
        protected float dir = -1f;
        protected float speed;
        protected float life;
        protected float age;

        public void Launch(float direction, float moveSpeed, float lifetime)
        {
            dir = direction;
            speed = moveSpeed;
            life = lifetime;
        }

        protected virtual void Tick()
        {
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            Tick();
        }
    }
}