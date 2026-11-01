using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySearchState : IState
{
   private enum SearchPhase
    {
        MovingToSearchPoint,
        LookingAround
    }
    private const int SearchHistoryCapacity = 8;
    private readonly EnemyController _owner;
    private readonly Vector3[] _searchedPositions = new Vector3[SearchHistoryCapacity];
    private SearchPhase _phase;

    private float _remainingMoveTime;
    private float _remainingLookTime;
    private float _remainingChaseRetryTime;
    private float _stimulusTimeOnEnter;
    private float _lookHalfAngle;
    private float _currentLookAngle;
    private float _targetLookAngle;
    private float _remainingLookPause;
    private bool _previousAgentUpdateRotation;
    private int _remainingSearchPoints;
    private int _searchedPositionCount;
    private Vector3 _searchCenter;
    private Quaternion _lookBaseRotation;
    public EnemySearchState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
    }
    public void Enter()
    {
        _previousAgentUpdateRotation = _owner.AgentUpdatesRotation;

        _stimulusTimeOnEnter = _owner.LastStimulusTime;
        _searchCenter = _owner.LastKnownPlayerPosition;
        _remainingSearchPoints = _owner.AdditionalSearchPoints;

        _remainingMoveTime = _owner.SearchMoveTimeout;
        _remainingLookTime = _owner.SearchLookDuration;
        _remainingChaseRetryTime = _owner.SearchChaseRetryDelay;
        _searchedPositionCount = 0;

        _phase = SearchPhase.MovingToSearchPoint;

        _owner.AgentUpdatesRotation = true;

        bool accepted = _owner.TryMoveTo(
            _searchCenter,
            _owner.SearchStoppingDistance);

        if (!accepted)
            BeginLookingAround();
    }
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;
        if (!_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }
        _remainingChaseRetryTime -= deltaTime;

        if (_owner.CanSeePlayer)
        {
            if (_remainingChaseRetryTime <= 0f)
                _owner.ChangeState(_owner.ChaseState);

            return;
        }
        else if (_owner.LastStimulusTime > _stimulusTimeOnEnter)
        {
            _owner.ChangeState(_owner.AlertState);
            return;
        }

        switch (_phase)
        {
            case SearchPhase.MovingToSearchPoint:
                TickMovement(deltaTime);
                break;
            
            case SearchPhase.LookingAround:
                TickLookAround(deltaTime);
                break;  
        }
    }
    public void Exit()
    {
        _owner.StopMoving();
        _owner.AgentUpdatesRotation = _previousAgentUpdateRotation;
    }

    private void TickMovement(float deltaTime)
    {
        _remainingMoveTime -= deltaTime;
        if(!_owner.CanNaviagate || _remainingMoveTime <= 0f)
        {
            BeginLookingAround();
            return;
        }
        if (_owner.IsPathPendinng)
        {
            return;
        }
        if(_owner.HasPathFailed() || _owner.HasReachedDestination())
        {
            BeginLookingAround();
        }
    }

   private void BeginLookingAround()
    {
        _owner.StopMoving();
        _owner.AgentUpdatesRotation = false;

        _phase = SearchPhase.LookingAround;
        _remainingLookTime = _owner.SearchLookDuration;

        // 이번 수색 지점에서의 정면을 한 번 저장합니다
        _lookBaseRotation = _owner.transform.rotation;

        _lookHalfAngle = _owner.SearchLookHalfAngle;
        _currentLookAngle = 0f;
        _remainingLookPause = 0f;

        // 처음 살펴볼 방향만 무작위로
        _targetLookAngle = UnityEngine.Random.value < 0.5f? -_lookHalfAngle : _lookHalfAngle;
    }
    private void TickLookAround(float deltaTime)
    {
        _remainingLookTime -= deltaTime;

        if (_remainingLookTime <= 0f)
        {
            RememberSearchedPosition();
            if (TryBeginNextSearchPoint())
                return;

            _owner.ChangeState(_owner.IdleState);
            return;
        }
        if (_lookHalfAngle <= 0f)
            return;

        // 좌우 끝에 도착하면 잠시 그 방향을 살펴봄
        if (_remainingLookPause > 0f)
        {
            _remainingLookPause = Mathf.Max(0f,_remainingLookPause - deltaTime);
            return;
        }         
        float turnSpeed = Mathf.Max(0f, _owner.SearchTurnSpeed);

        if (turnSpeed <= 0f)
            return;
        

        float nextAngle = Mathf.MoveTowards( _currentLookAngle,_targetLookAngle,turnSpeed * deltaTime);
        
        // 실제로 각도가 바뀔 때만 Transform을 변경
        if (!Mathf.Approximately(nextAngle, _currentLookAngle))
        {
            _currentLookAngle = nextAngle;
            // 현재 회전에 누적하지 않고 저장한 정면을 기준으로 계산
            _owner.transform.rotation = Quaternion.AngleAxis(_currentLookAngle, Vector3.up) * _lookBaseRotation;
        }
        
        if (Mathf.Approximately(_currentLookAngle, _targetLookAngle))
        {
            _remainingLookPause = _owner.SearchLookPauseDuration;
            _targetLookAngle = -_targetLookAngle;
        }
    }
    private void RememberSearchedPosition()
    {
        if (_searchedPositionCount >= _searchedPositions.Length)
            return;

        // 요청했던 목적지가 아니라 실제로 둘러본 위치를 기록합니다.
        _searchedPositions[_searchedPositionCount] = _owner.transform.position;

        _searchedPositionCount++;
    }
    private bool TryBeginNextSearchPoint()
    {
       if (_remainingSearchPoints <= 0)
        return false;

        if (!_owner.CanNaviagate)
            return false;

        if (_searchedPositionCount >= _searchedPositions.Length)
            return false;

        bool accepted = _owner.TryMoveToAreaSearchPoint( _searchCenter, _searchedPositions,_searchedPositionCount);

        if (!accepted)
            return false;

        _remainingSearchPoints--;

        _owner.AgentUpdatesRotation = true;

        _phase = SearchPhase.MovingToSearchPoint;
        _remainingMoveTime = _owner.SearchMoveTimeout;

        return true;
    }
}
