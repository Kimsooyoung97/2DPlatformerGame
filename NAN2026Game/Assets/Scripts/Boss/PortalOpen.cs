using UnityEngine;

public class PortalOpen : MonoBehaviour
{
    [SerializeField] private GameObject portal; 
    // Update is called once per frame
    void Update()
    {
        if (gameObject.IsDestroying())
        {
            portal.SetActive(true);
        }
    }
}
