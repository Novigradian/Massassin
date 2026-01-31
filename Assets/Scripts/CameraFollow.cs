using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float height;
    public float followSpeed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPosition = new Vector3(
            target.position.x,
            height,
            target.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
}
