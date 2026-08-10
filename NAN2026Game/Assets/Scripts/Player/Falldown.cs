using UnityEngine;

public class Falldown : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            player.Kill();
        }
        else
        {
            Destroy(collision.gameObject);
        }
    }
}
