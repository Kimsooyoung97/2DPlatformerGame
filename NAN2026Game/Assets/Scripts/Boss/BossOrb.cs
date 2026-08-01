using UnityEngine;

namespace NAN2026
{
    public class BossOrb : MonoBehaviour
    {
        private float dir = -1f;
        private float speed;
        private float life;
        private float age;

        public void Launch(float direction, float moveSpeed, float lifetime)
        {
            dir = direction;
            speed = moveSpeed;
            life = lifetime;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        }
    }
}