using Unity.Cinemachine;
using UnityEngine;

public class CameraTracking : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    private Transform player;
    void Awake()
    {
        player = GameObject.Find("RealPlayer").transform;
        cam.Follow = player;
    }
}
