using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolRoute : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    public int Count => waypoints != null ? waypoints.Length : 0;

    public bool TryGetPosition(int index, out Vector3 position)
    {
        position = default;
        if(waypoints == null || index < 0 || index >= waypoints.Length)
        {
            return false;
        }
        Transform waypoint = waypoints[index];

        if(waypoint == null)
        {
            return false;
        }
        position = waypoint.position;
        return true;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(waypoints == null)
        {
            return;
        }
        Gizmos.color = Color.yellow;
        for(int i = 0; i < waypoints.Length; i++)
        {
            Transform current = waypoints[i];
            if(current == null) continue;
            Gizmos.DrawWireSphere(current.position, 0.3f);

            int nextIndex = (i + 1) % waypoints.Length;
            Transform next = waypoints[nextIndex];
            if(next != null)
            {
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
#endif
}
