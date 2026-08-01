using UnityEngine;

namespace NAN2026
{
    public class FloatingText : MonoBehaviour
    {
        private float age;
        private const float Life = 0.8f;
        private const float RiseSpeed = 1.2f;
        private TextMesh tm;

        public static void Spawn(Vector3 position, string text, Color color)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;
            var t = go.AddComponent<TextMesh>();
            t.text = text;
            t.characterSize = 0.15f;
            t.fontSize = 48;
            t.anchor = TextAnchor.MiddleCenter;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.GetComponent<MeshRenderer>().material = t.font.material;
            go.AddComponent<FloatingText>().tm = t;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= Life) { Destroy(gameObject); return; }
            transform.position += new Vector3(0f, RiseSpeed * Time.deltaTime, 0f);
            if (tm != null)
            {
                var c = tm.color;
                c.a = 1f - age / Life;
                tm.color = c;
            }
        }
    }
}