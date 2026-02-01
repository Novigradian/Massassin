using UnityEngine;
using System.Collections.Generic;

public class EnemyVision : MonoBehaviour
{
    [Header("Cone Settings")]
    public float viewRadius = 15f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("Layer Masks")]
    public LayerMask targetMask;   // Player / Player Adjacent
    public LayerMask obstacleMask; // Environment

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();

    private Coroutine visionRoutine;

    void Start()
    {
        
        //StartCoroutine(FindTargetsWithDelay(0.1f));
    }

    void OnEnable()
    {
        visionRoutine = StartCoroutine(FindTargetsWithDelay(0.1f));
    }

    void OnDisable()
    {
        if (visionRoutine != null)
            StopCoroutine(visionRoutine);

        visibleTargets.Clear();
    }

    System.Collections.IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                    Debug.Log("Player Detected!");
                    // Call your Chase script here

                }
            }
        }
    }

    // --- DEBUG VISUALIZATION (GIZMOS) ---
    // This draws the cone in the Scene view for easy editing
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        Gizmos.color = Color.red;
        foreach (Transform visibleTarget in visibleTargets)
        {
            Gizmos.DrawLine(transform.position, visibleTarget.position);
        }
    }
}