using NAN2026;
using UnityEngine;

public class PortalOpen2 : MonoBehaviour
{
    [SerializeField] private GameObject portal;
    DemonBoss tmp;
    bool check;
    // Update is called once per frame
    private void Awake()
    {
        tmp = gameObject.GetComponent<DemonBoss>();
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
