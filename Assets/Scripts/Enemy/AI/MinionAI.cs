using UnityEngine;
using UnityEngine.AI;

public class MinionAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform targetEnemy;

    private MinionMask owner;

    public GameObject mask;

    void Start()
    {
        mask.SetActive(false);
    }

    public void Initialize(MinionMask owner)
    {
        agent = GetComponent<NavMeshAgent>();
        this.owner = owner;

        GetComponent<NavMeshPatrol>().enabled = false;
        GetComponent<EnemyVision>().enabled = false;
        agent.ResetPath();

        mask.SetActive(true);

        GameObject target = GameObject.FindGameObjectWithTag("TargetEnemy");
        if (target != null)
            targetEnemy = target.transform;
            
    }

    void Update()
    {
        if (!agent || !targetEnemy) return;

        agent.SetDestination(targetEnemy.position);
    }

    public void Die()
    {
        owner.transform.position = transform.position;
        Destroy(gameObject);
    }
}
