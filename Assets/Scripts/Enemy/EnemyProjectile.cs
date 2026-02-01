using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f; // Destroy after 5s to save memory

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 1. IGNORE GRAVITY so it flies straight
        rb.useGravity = false; 

        // 2. SET VELOCITY (Fly forward relative to how it was spawned)
        rb.linearVelocity = transform.forward * speed;

        // 3. CLEANUP (Destroy bullet after X seconds if it hits nothing)
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {

        // If it hits the player...
        if (other.CompareTag("Mask"))
        {
            Debug.Log("You Lose!");
            // other.GetComponent<PlayerHealth>().TakeDamage(10);

            //GameManager.Instance.HandleLose();
        }

        if (other.CompareTag("Enemy"))
        {
            Possessable p = other.GetComponent<Possessable>();
            if (p == null) return;

            if (p.isPossessed)
            {
                Debug.Log("Hit Possessed Enemy.");  
                p.PossessedDie();
            }

            if (p.isMinioned)
            {
                p.gameObject.GetComponent<MinionAI>().Die();
            }
        }

        // Destroy bullet on impact with anything (walls, floor, player)
        Destroy(gameObject);
    }
}