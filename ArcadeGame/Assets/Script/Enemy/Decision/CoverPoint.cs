using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoverPoint : MonoBehaviour
{

    [Header("Peek Positions")]
    [SerializeField] private Transform leftPeekPoint;
    [SerializeField] private Transform rightPeekPoint;
    private EnemyController _reservedBy;
    public Vector3 Position => transform.position;
    public Transform LeftPeekPoint => leftPeekPoint;
    public Transform RightPeekPoint => rightPeekPoint;
    
    public bool IsAvailableFor(EnemyController requester)
    {
        if(requester == null || !isActiveAndEnabled) return false;

        // 비활성화되거나 죽은 적은 예약에서 회수
        if(_reservedBy == null || !_reservedBy.isActiveAndEnabled) _reservedBy = null;

        return _reservedBy == null || _reservedBy == requester;
    }
    
    public bool TryReserve(EnemyController requester)
    {
        if(!IsAvailableFor(requester)) return false;
        _reservedBy = requester;

        return true;
    }

    public bool IsReservedBy(EnemyController requrster)
    {
        return  requrster != null && _reservedBy == requrster;
    }
    public void Release(EnemyController requester)
    {
        //다른 적이 예약을 해제하지 못하도록 소유자를 확인
        if(requester != null && _reservedBy == requester)
            _reservedBy = null;
    }

    private void OnEnable()
    {
        _reservedBy = null;
        CoverRegistry.Register(this);        
    }
    private void OnDisable()
    {
        CoverRegistry.Unregister(this);
        _reservedBy = null;
    }

    internal void ResetReservationForSession()
    {
        _reservedBy = null;
    }
}
