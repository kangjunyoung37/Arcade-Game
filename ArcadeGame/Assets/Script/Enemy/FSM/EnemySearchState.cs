using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySearchState : IState
{
    private enum SearchPhase
    {
        MovingToLastKnownPosition,
        LookingAround,
    }
    private readonly EnemyController _owner;
    private SearchPhase _phase;

    private float _remainingMoveTime;
    private float _remainingLookTime;
    private float _remainingChaseRetryTime;
    
    private float _stimulusTimeOnEnter;
    private bool _previousAgetnUpdateRotation;

    public EnemySearchState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
    }
    public void Enter()
    {
        
    }
    public void Tick(float deltaTime)
    {
        
    }
    public void Exit()
    {
        
    }
}
