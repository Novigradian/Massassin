using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class MinionMask : MonoBehaviour
{
    public float throwSpeed;
    public float arriveDistance = 0.1f;

    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private LayerMask possessableLayer;

    private Vector3 moveDirection;

    private Vector3 targetPosition;
    private bool isFlying;
    private bool isLanded;
    //private bool isPossessing;
    //private bool isRecalling;

    private Rigidbody rb;
    //private NavMeshAgent agent;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //agent = GetComponent<NavMeshAgent>();
    }

    /*public void Launch(Vector3 target)
    {
        targetPosition = target;
        targetPosition.y = transform.position.y;
        isFlying = true;

        rb.isKinematic = false;
        agent.enabled = false;
    }*/
    public void Launch(Vector3 direction)
    {
        moveDirection = direction.normalized;
        isFlying = true;
        //isRecalling = false;

        rb.isKinematic = false;
        rb.linearVelocity = moveDirection * throwSpeed;
    }

    void FixedUpdate()
    {
        if (!isFlying) return;

        /*Vector3 toTarget = targetPosition - rb.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            Land();
            return;
        }

        rb.linearVelocity = toTarget.normalized * throwSpeed;*/
        if (!isFlying) return;

    rb.linearVelocity = moveDirection * throwSpeed;
    }

    /*void Land()
    {
        isFlying = false;

        Debug.Log("landed");

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true; // 🔥 THIS STOPS THE SHAKE

        transform.position = targetPosition; // authoritative snap
    }*/

    void Land()
    {
        isFlying = false;
        isLanded = true;

        Debug.Log("landed");

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    /*void OnTriggerEnter(Collider other)
    {
        if (isFlying || isRecalling) return;

        if (other.TryGetComponent<Possessable>(out var possessable))
        {
            PossessEnemy(other.transform);
        }
    }*/

    void PossessEnemy(Transform enemy)
    {
        //agent.enabled = true;
        //agent.isStopped = false;

        MinionAI ai = enemy.gameObject.GetComponent<MinionAI>();
        ai.Initialize(this);

        // mask visually attaches or disappears
        gameObject.SetActive(false);
    }

    /*public void Recall(Vector3 returnTarget)
    {
        isRecalling = true;
        isFlying = false;

        agent.enabled = false;
        rb.isKinematic = false;

        targetPosition = returnTarget;
        isFlying = true;
    }

    void OnReachedRecallTarget()
    {
        Debug.Log("Recalled");
        
        Destroy(gameObject);
    }*/

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == MaskController.Instance.controlledTarget.gameObject)
        {
            MaskController.Instance.PickUpMinion(gameObject);
            return;
        }
        
        //if (!isFlying || isRecalling) return;
        if (!isFlying) return;

        Land();
    }

    void OnTriggerEnter(Collider other)
    {
        //if (!isFlying || isRecalling) return;
        if (!isFlying) return;

        /*if (((1 << other.gameObject.layer) & possessableLayer) == 0)
            return;*/
        
        /*if (other.gameObject == MaskController.Instance.controlledTarget.gameObject)
        {
            //pickup
        }*/
            

        if (other.gameObject.CompareTag("Enemy") && other.TryGetComponent<Possessable>(out var possessable))
        {
            PossessEnemy(other.transform);
            //Debug.Log("Possessable hit while flying, but possession is disabled.");
        }

        
    }

}
