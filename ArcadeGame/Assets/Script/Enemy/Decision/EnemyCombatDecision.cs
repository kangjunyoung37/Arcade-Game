using UnityEngine;

public enum EnemyCombatMoveIntent
{
    Strafe,
    Retreat
}
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyCombatDecision : MonoBehaviour
{
    private const float MinimumDistance = 0.1f;
    private const float MinimumDistanceGap = 0.5f;
    [SerializeField, Min(MinimumDistance)] private float retreatEnterDistance = 4f;
    [SerializeField, Min(MinimumDistance)] private float retreatExitDistance = 6f;
    private EnemyController _owner;
    public EnemyCombatMoveIntent Intent {get; private set;}

    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
    }
    public void ResetDecision()
    {
        Intent = EnemyCombatMoveIntent.Strafe;
    }

    public EnemyCombatMoveIntent Evaluate()
    {
        if(_owner == null || !_owner.HasTarget || !_owner.CanSeePlayer)
        {
            ResetDecision();
            return Intent;
        }
        Vector3 difference = _owner.LastKnownPlayerPosition - transform.position;
        difference.y = 0f;
        float distanceSquared = difference.sqrMagnitude;
        float enterDistance = Mathf.Max(MinimumDistance, retreatEnterDistance);
        float exitDistance = Mathf.Max(enterDistance + MinimumDistanceGap, retreatExitDistance);
        if(Intent == EnemyCombatMoveIntent.Retreat)
        {
            if(distanceSquared >= exitDistance * exitDistance)
                Intent = EnemyCombatMoveIntent.Strafe;
        }
        else if(distanceSquared <= enterDistance * enterDistance)
        {
            Intent = EnemyCombatMoveIntent.Retreat;
        }
        return Intent;
    }
}
