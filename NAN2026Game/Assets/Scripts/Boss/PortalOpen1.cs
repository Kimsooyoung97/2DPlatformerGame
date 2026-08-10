using NAN2026;
using UnityEngine;

public class PortalOpen4 : MonoBehaviour
{
    [SerializeField] private GameObject portal;
    EnemyAI tmp;
    bool check;
    // Update is called once per frame
    private void Awake()
    {
        tmp = gameObject.GetComponent<EnemyAI>();
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
