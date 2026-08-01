using UnityEngine;

namespace NAN2026
{
    public class BossOrb : MonoBehaviour
    {
        protected Vector2 moveDir = Vector2.left;
        protected float speed;
        protected float life;
        protected float age;

        public void Launch(float direction, float moveSpeed, float lifetime)
        {
            moveDir = new Vector2(direction, 0f);
            speed = moveSpeed;
            life = lifetime;
        }

        public void LaunchAt(Vector3 targetPos, float moveSpeed, float lifetime)
        {
            Vector2 d = targetPos - transform.position;
            moveDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.left;
            speed = moveSpeed;
            life = lifetime;
        }

        protected virtual void Tick()
        {
            transform.position += (Vector3)(moveDir * (speed * Time.deltaTime));
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            Tick();
        }
    }
}