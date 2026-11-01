using UnityEngine;

public enum EnemyCombatMoveIntent
{
    Strafe,
    Retreat,
    Approach,
    Hold,
    LinearMove
}
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyCombatDecision : MonoBehaviour
{
    private const float MinimumDistance = 0.1f;
    private const float MinimumDistanceGap = 0.5f;
    private const float MinimumDuration = 0.1f;
    private const float MinimumRepeatMultiplier = 0.1f;

    [Header("Retreat")]
    [SerializeField, Min(MinimumDistance)] private float retreatEnterDistance = 4f;
    [SerializeField, Min(MinimumDistance)] private float retreatExitDistance = 6f;
    
    [Header("Approach")]
    [SerializeField, Min(MinimumDistance)] private float approachEnterDistance = 7.5f;
    [SerializeField, Min(MinimumDistance)] private float approachStopDistance = 6.5f;

    [Header("Pattern Duration")]
    [SerializeField, Min(MinimumDuration)] private float  minPatternDuration = 1.2f;
    [SerializeField, Min(MinimumDuration)] private float maxPatternDuration = 2.5f;

    [Header("Pattern Weights")]
    [SerializeField, Min(0f)] private float strafeWeight = 5f;
    [SerializeField, Min(0f)] private float approachWeight = 4f;
    [SerializeField, Min(0f)] private float holdWeight = 1f;
    [Tooltip("직전 행동의 가충치에 곲하는 값")]
    [SerializeField, Range(MinimumRepeatMultiplier , 1f)] private float repeatWeightMultiplier = 0.35f;

    [Header("Movement Failure")]
    [SerializeField, Min(MinimumDuration)] private float blockedPatternCooldown = 2f;

    [Header("Pressure Response")]
    [SerializeField, Min(1f)] private float pressureStrafeMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float pressureHoldMultiplier = 0.25f;

    [Header("Linear Move Decision")]
    [SerializeField, Min(MinimumDuration)] private float minLinearMoveInterval = 3f;
    [SerializeField, Min(MinimumDuration)] private float maxLinearMoveInterval = 6f;
    [SerializeField, Range(0f, 1f)]private float linearMoveChance = 0.5f;

    private EnemyController _owner;
    private float _remainingPatternTime;
    private float _strafeBlockedUntil;
    private float _approachBlockedUntil;
    private float _nextLinearMoveDecisionTime;
    private bool _hasSelection;
    public EnemyCombatMoveIntent Intent {get; private set;}
    private float RemainingPatternTime => _remainingPatternTime;
    private float RetreatEnterDistance => Mathf.Max(MinimumDistance, retreatEnterDistance);
    private float RetreatExitDistance => Mathf.Max(RetreatEnterDistance + MinimumDistanceGap, retreatExitDistance);
    public float ApproachStopDistance => Mathf.Max(RetreatExitDistance + MinimumDistanceGap, approachStopDistance);
    public float ApproachEnterDistance => Mathf.Max(ApproachStopDistance + MinimumDistanceGap, approachEnterDistance);
    private float CurrentPressure
    {
        get
        { 
            EnemyCombatPressure pressure = _owner != null ? _owner.CombatPressure : null;
            return pressure != null && pressure.isActiveAndEnabled ? pressure.PressureNormalized : 0f;
        }
    }
    private bool IsUnderHighPressure
    {
        get
        {
            EnemyCombatPressure pressure = _owner != null ? _owner.CombatPressure : null;
            return pressure != null && pressure.isActiveAndEnabled && pressure.IsHighPressure;
        }
    }

    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
    }
    public void ResetDecision()
    {
        Intent = EnemyCombatMoveIntent.Hold;
        _remainingPatternTime = 0f;
        _hasSelection = false;
        if (_owner != null && _owner.CanUseLinearMove && Time.time >= _nextLinearMoveDecisionTime)
            ScheduleNextLinearMoveDecision();
 
    }

    public EnemyCombatMoveIntent Evaluate(float deltaTime)
    {
        if(_owner == null || !_owner.HasTarget || !_owner.CanSeePlayer)
        {
            ResetDecision();
            return Intent;
        }
        if(deltaTime <= 0f)
            return Intent;
        EnemyStrafeMovement movement = _owner.StrafeMovement;
        if(movement == null || !movement.CanExecuteCombatMovement)
        {
            ResetDecision();
            return Intent;
        }

        Vector3 difference = _owner.LastKnownPlayerPosition - transform.position;
        difference.y = 0f;
        float distanceSquared = difference.sqrMagnitude;

        if(ShouldPrioritizeRetreat())
        {
            Intent = EnemyCombatMoveIntent.Retreat;
            _remainingPatternTime = 0f;
            return Intent;
        }
        
        float now = Time.time;

        // 도착 또는 실패할 때까지 같은 Intent를 유지합니다
        if (Intent == EnemyCombatMoveIntent.LinearMove)
        {
            if (_owner.CanUseLinearMove && !movement.LinearMoveFinished)
                return Intent;

            // 이동이 끝나면 반드시 Hold를 한 번 거칩니다
            // 같은 LinearMove가 연속 선택되어 초기화되지 않는 문제를 방지
            Intent = EnemyCombatMoveIntent.Hold;
            _remainingPatternTime = 0f;

            ScheduleNextLinearMoveDecision();
            return Intent;
        }

        bool canSelectLinearMove = Intent == EnemyCombatMoveIntent.Hold || _remainingPatternTime <= deltaTime;
        // 진행 중인 다른 패턴이 끝나는 시점에만 새 이동을 선택합니다.
        if (_owner.CanUseLinearMove && now >= _nextLinearMoveDecisionTime && canSelectLinearMove)
        {
            ScheduleNextLinearMoveDecision();
            float chance = Mathf.Clamp01(linearMoveChance);

            bool selected = chance > 0f && (chance >= 1f || Random.value < chance);

            if (selected)
            {
                Intent = EnemyCombatMoveIntent.LinearMove;
                _remainingPatternTime = 0f;
                _hasSelection = true;

                return Intent;
            }
        }

        bool strafeAllowed = _owner.CanUseStrafe && now >= _strafeBlockedUntil;
        bool approachAllowed =  _owner.CanUseApproach && now >= _approachBlockedUntil && !IsUnderHighPressure;

        float stopDistance = ApproachStopDistance;
        
        bool approachFinished = Intent == EnemyCombatMoveIntent.Approach && (distanceSquared <= stopDistance * stopDistance || movement.ApproachDestinationReached);

        bool currentInvalid = Intent == EnemyCombatMoveIntent.Retreat || (Intent == EnemyCombatMoveIntent.Strafe && !strafeAllowed) || (Intent == EnemyCombatMoveIntent.Approach && (!approachAllowed || approachFinished));

        _remainingPatternTime = Mathf.Max(0f, _remainingPatternTime - deltaTime);

        if(!currentInvalid && _remainingPatternTime > 0f)
            return Intent;
        
        float enterDistance = ApproachEnterDistance;
        
        bool canStartApproach = approachAllowed && !approachFinished && distanceSquared >= enterDistance * enterDistance;

        SelectPattern(strafeAllowed, canStartApproach);
        return Intent;
    }
    private void SelectPattern(bool strafeAllowed, bool approachAllowed)
    {
        float strafe = strafeAllowed ? Mathf.Max(0f, strafeWeight) : 0f;
        float approach = approachAllowed ? Mathf.Max(0f, approachWeight) : 0f;

        float hold = Mathf.Max(0f, holdWeight);
        float pressure = CurrentPressure;

        approach *= 1f - pressure;

        strafe *= Mathf.Lerp(1f, Mathf.Max(1f, pressureStrafeMultiplier),pressure);
        
        hold *= Mathf.Lerp(1f, Mathf.Clamp01(pressureHoldMultiplier), pressure);
        if (_hasSelection)
        {
            float multiplier = Mathf.Clamp(repeatWeightMultiplier, MinimumRepeatMultiplier, 1f);
            switch(Intent)
            {
                case EnemyCombatMoveIntent.Strafe:
                    strafe *= multiplier;
                    break;
                case EnemyCombatMoveIntent.Approach:
                    approach *= multiplier;
                    break;
                case EnemyCombatMoveIntent.Hold:
                    hold *= multiplier;
                    break;
            }
        }

        Intent = PickWeighted(strafe, approach, hold);
        _hasSelection = true;

        float minimum = Mathf.Max(MinimumDuration, minPatternDuration);
        float maximum = Mathf.Max(minimum, maxPatternDuration);
        
        _remainingPatternTime = Random.Range(minimum, maximum);
    }
    private static EnemyCombatMoveIntent PickWeighted(float strafe, float approach, float hold)
    {
        float total = strafe + approach + hold;

        if(total <= 0f)
            return EnemyCombatMoveIntent.Hold;

        float value = Random.Range(0f, total);

        if(value < strafe)
            return EnemyCombatMoveIntent.Strafe;
        
        value -= strafe;

        if(value < approach)
            return EnemyCombatMoveIntent.Approach;
        
        if(hold > 0f)
            return EnemyCombatMoveIntent.Hold;
        
        //난수가 최댓값인 경우에도 가중치 0인 행동은 선택 x
        return approach > 0f ? EnemyCombatMoveIntent.Approach : EnemyCombatMoveIntent.Strafe;
    }

    private void ScheduleNextLinearMoveDecision()
    {
        float minimum = Mathf.Max( MinimumDuration, minLinearMoveInterval);

        float maximum = Mathf.Max( minimum, maxLinearMoveInterval);

        _nextLinearMoveDecisionTime = Time.time + Random.Range(minimum, maximum);
    }
    
    public void ReportMovmentBlocked(EnemyCombatMoveIntent intent)
    {
        float until = Time.time + Mathf.Max(MinimumDuration, blockedPatternCooldown);
        switch (intent)
        {
            case EnemyCombatMoveIntent.Strafe:
                _strafeBlockedUntil = until;
                break;
            case EnemyCombatMoveIntent.Approach:
                _approachBlockedUntil = until;
                break;
            default:
                return;
        }
        if(Intent == intent)
            _remainingPatternTime = 0f;
    }

    public bool ShouldPrioritizeRetreat()
    {
        if(!isActiveAndEnabled || _owner == null || !_owner.CanUseRetreat || !_owner.HasTarget || !_owner.CanSeePlayer)
            return false;
        
        Vector3 difference = _owner.LastKnownPlayerPosition - transform.position;
        difference.y = 0f;

        float distanceSquared = difference.sqrMagnitude;

        if(Intent == EnemyCombatMoveIntent.Retreat)
        {
            float exitDistance = RetreatExitDistance;

            return distanceSquared < exitDistance * exitDistance;
        }
        float enterDistance = RetreatEnterDistance;

        return distanceSquared <= enterDistance * enterDistance;
    }
}
