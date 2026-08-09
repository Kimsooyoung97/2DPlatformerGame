using NAN2026;
using UnityEngine;

public class PortalOpen : MonoBehaviour
{
    [SerializeField] private GameObject portal;
    MidBoss_FireKnight tmp;
    bool check;
    // Update is called once per frame
    private void Awake()
    {
        tmp = gameObject.GetComponent<MidBoss_FireKnight>();
        check = tmp.death;
    }
    void Update()
    {
        check = tmp.death;
        if (check)
        {
            portal.SetActive(true);
        }
    }
}
