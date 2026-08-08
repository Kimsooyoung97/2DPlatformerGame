using UnityEngine;
using static UnityEngine.Rendering.STP;

public class TmpAddEXP : MonoBehaviour
{
    public GameObject player;
    public void AddEXP()
    {
        PlayerProgression progression = player.GetComponentInParent<PlayerProgression>();
        if (progression != null) progression.AddXp(10);
    }
    
}
