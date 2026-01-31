using UnityEngine;

public class Possessable : MonoBehaviour
{
    public bool isPossessed = false;

    public MaskController maskController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isPossessed) return;
        
        maskController.TryPossessTarget(other);
    }
}
