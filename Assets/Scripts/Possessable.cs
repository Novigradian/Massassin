using UnityEngine;

public class Possessable : MonoBehaviour
{
    public bool isPossessed = false;

    public MaskController maskController;

    public GameObject mask;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ToggleMask(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isPossessed) return;
        
        maskController.TryPossessTarget(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isPossessed) return;
        
        maskController.TryPossessTarget(collision.gameObject);
    }

    public void ToggleMask(bool state)
    {
        if (mask == null) return;

        mask.SetActive(state);
    }
}
