using UnityEngine;

public class PlayerSapwn : MonoBehaviour
{
    [SerializeField]private Transform spawnPoint;
    private Transform player;
    void Awake()
    {
        player = GameObject.Find("RealPlayer").transform;
        player.position = spawnPoint.position;
    }

}
