using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NavMeshPatrol_WithAnimation))]
public class NavMeshPatrol_WithAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavMeshPatrol_WithAnimation script = (NavMeshPatrol_WithAnimation)target;

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Copy from NavMeshPatrol Component"))
        {
            NavMeshPatrol oldScript = script.GetComponent<NavMeshPatrol>();
            
            if (oldScript != null)
            {
                // Use SerializedObject to copy properties
                SerializedObject oldSO = new SerializedObject(oldScript);
                SerializedObject newSO = new SerializedObject(script);

                // Copy Combat Settings
                newSO.FindProperty("combatType").enumValueIndex = oldSO.FindProperty("combatType").enumValueIndex;
                newSO.FindProperty("attackRange").floatValue = oldSO.FindProperty("attackRange").floatValue;
                newSO.FindProperty("timeBetweenAttacks").floatValue = oldSO.FindProperty("timeBetweenAttacks").floatValue;
                
                // Copy projectile prefab and fire point
                newSO.FindProperty("projectilePrefab").objectReferenceValue = oldSO.FindProperty("projectilePrefab").objectReferenceValue;
                newSO.FindProperty("firePoint").objectReferenceValue = oldSO.FindProperty("firePoint").objectReferenceValue;

                // Copy patrol points
                SerializedProperty oldPatrolPoints = oldSO.FindProperty("patrolPoints");
                SerializedProperty newPatrolPoints = newSO.FindProperty("patrolPoints");
                newPatrolPoints.arraySize = oldPatrolPoints.arraySize;
                for (int i = 0; i < oldPatrolPoints.arraySize; i++)
                {
                    newPatrolPoints.GetArrayElementAtIndex(i).objectReferenceValue = 
                        oldPatrolPoints.GetArrayElementAtIndex(i).objectReferenceValue;
                }

                // Copy Patrol Settings
                newSO.FindProperty("patrolSpeed").floatValue = oldSO.FindProperty("patrolSpeed").floatValue;
                newSO.FindProperty("waitTime").floatValue = oldSO.FindProperty("waitTime").floatValue;
                newSO.FindProperty("turnSpeed").floatValue = oldSO.FindProperty("turnSpeed").floatValue;
                newSO.FindProperty("lookAngle").floatValue = oldSO.FindProperty("lookAngle").floatValue;

                // Copy Chase Settings
                newSO.FindProperty("chaseSpeed").floatValue = oldSO.FindProperty("chaseSpeed").floatValue;

                // Get Animator component
                Animator animator = script.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = script.gameObject.AddComponent<Animator>();
                }

                newSO.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(script);
                
                Debug.Log("Successfully copied all properties from NavMeshPatrol component and got Animator!");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No NavMeshPatrol component found on this GameObject!", "OK");
            }
        }
    }
}

