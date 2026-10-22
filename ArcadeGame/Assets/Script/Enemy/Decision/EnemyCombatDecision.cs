using UnityEngine;

public enum EnemyCombatMoveIntent
{
    Strafe,
    Retreat,
    Approach
}
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyCombatDecision : MonoBehaviour
{
    private const float MinimumDistance = 0.1f;
    private const float MinimumDistanceGap = 0.5f;
    [Header("Retreat")]
    [SerializeField, Min(MinimumDistance)] private float retreatEnterDistance = 4f;
    [SerializeField, Min(MinimumDistance)] private float retreatExitDistance = 6f;
    [Header("Approach")]
    [SerializeField, Min(MinimumDistance)] private float approachEnterDistance = 7.5f;
    [SerializeField, Min(MinimumDistance)] private float approachStopDistance = 6.5f;
    private EnemyController _owner;
    public EnemyCombatMoveIntent Intent {get; private set;}

    private float RetreatEnterDistance => Mathf.Max(MinimumDistance, retreatEnterDistance);
    private float RetreatExitDistance => Mathf.Max(RetreatEnterDistance + MinimumDistanceGap, retreatExitDistance);
    public float ApproachStopDistance => Mathf.Max(RetreatExitDistance + MinimumDistanceGap, approachStopDistance);
    public float ApproachEnterDistance => Mathf.Max(ApproachStopDistance + MinimumDistanceGap, approachEnterDistance);

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
        if(Intent == EnemyCombatMoveIntent.Retreat)
        {
            float exitDistance = RetreatExitDistance;
            if(distanceSquared >= exitDistance * exitDistance)
                Intent = EnemyCombatMoveIntent.Strafe;
            return Intent;
        }
        float retreatDistance = RetreatEnterDistance;

        if(distanceSquared <= retreatDistance  * retreatDistance )
        {
            Intent = EnemyCombatMoveIntent.Retreat;
            return Intent;
        }
        if(Intent == EnemyCombatMoveIntent.Approach)
        {
            float stopDistance = ApproachStopDistance;

            bool reachedDistance = distanceSquared <= stopDistance * stopDistance;

            bool reachedDestination = _owner.StrafeMovement != null && _owner.StrafeMovement.ApproachDestinationReached;
            if(reachedDistance || reachedDestination)
                Intent = EnemyCombatMoveIntent.Strafe;
            return Intent;
        }
        float enterDistance = ApproachEnterDistance;
        if(distanceSquared >= enterDistance * enterDistance)
            Intent = EnemyCombatMoveIntent.Approach;
        return Intent;
    }
}
