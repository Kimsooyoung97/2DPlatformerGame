using NAN2026;
using UnityEngine;

public class PortalOpen1 : MonoBehaviour
{
    [SerializeField] private GameObject portal;
    MinoBoss tmp;
    bool check;
    // Update is called once per frame
    private void Awake()
    {
        tmp = gameObject.GetComponent<MinoBoss>();
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
