using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public Image target;
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = true;

    private float animT;

    private void Update()
    {
        if (target == null || frames == null || frames.Length == 0) return;

        animT += Time.unscaledDeltaTime * fps;
        int idx = loop
            ? (int)animT % frames.Length
            : Mathf.Min((int)animT, frames.Length - 1);

        target.sprite = frames[idx];
    }
}