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
        GetComponent<Possessable>().isMinioned = true;
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
        owner.gameObject.SetActive(true);
        owner.Land();
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TargetEnemy"))
        {
            // Implement logic for when the minion reaches the target enemy
            Debug.Log("Minion has reached the target enemy!");
            // For example, deal damage or trigger an event
            GameManager.Instance.HandleWin();
        }
    }
}
