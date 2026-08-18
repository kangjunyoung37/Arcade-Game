using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

#if UNITY_EDITOR

    private const float GizmoRadius = 0.5f;
    private const float DirectionLength = 2f;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, GizmoRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * DirectionLength);
    }
#endif
}
