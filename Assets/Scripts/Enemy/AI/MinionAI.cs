using UnityEngine;
using UnityEngine.AI;

public class MinionAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform targetEnemy;

    public void Initialize(MinionMask owner)
    {
        agent = GetComponent<NavMeshAgent>();
        FindTarget();
    }

    void FindTarget()
    {
        GameObject target = GameObject.FindGameObjectWithTag("TargetEnemy");
        if (target != null)
            targetEnemy = target.transform;
    }

    void Update()
    {
        if (!agent || !targetEnemy) return;

        agent.SetDestination(targetEnemy.position);
    }
}
