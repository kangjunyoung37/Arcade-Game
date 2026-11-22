using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazinePackingController : MonoBehaviour
{
    private const float MinimumSecondsPerRound = 0.05f;

    [Header("References")]
    [SerializeField] private MagazineInventoryPresenter presenter;
    [SerializeField] private GameObject inventoryWindow;

    [Header("Packing")]
    [SerializeField, Min(MinimumSecondsPerRound)]
    private float secondsPerRound = 0.5f;

    private MagazineInventory _inventory;
    private MagazineItemInstance _target;

    private int _targetIndex = -1;
    private float _roundDuration;
    private float _remainingSeconds;

    // 탄창 Changed 이벤트 처리 중 새로운 작업이 시작되는 것을 막습니다.
    private bool _isTransferring;

    public bool IsPacking => _target != null;

    // 탄창 전체가 아니라 현재 넣고 있는 한 발의 진행률입니다.
    public float CurrentRoundProgress => IsPacking && _roundDuration > 0f ? Mathf.Clamp01(1f - _remainingSeconds / _roundDuration) : 0f;
    public float CurrentRoundRemainingSeconds => IsPacking ? Mathf.Max(0f, _remainingSeconds) : 0f;

    // 작업 시작·종료 알림입니다. 매 프레임 호출하지 않습니다.
    public event Action StateChanged;

    private bool IsInventoryOpen => inventoryWindow != null && inventoryWindow.activeInHierarchy;

    /// <summary>
    /// 지정한 인벤토리 탄창에 연속 삽입 작업을 시작합니다.
    /// 시작 시에는 탄약을 소모하지 않습니다.
    /// </summary>
    public bool TryStartPacking(int inventoryIndex)
    {
        if (!isActiveAndEnabled || IsPacking || _isTransferring || !IsInventoryOpen || Time.timeScale <= 0f || presenter == null)
            return false;

        MagazineInventory inventory = presenter.Inventory;

        if (inventory == null || !inventory.isActiveAndEnabled)
            return false;

        if (!presenter.TryGetBoundItem( inventoryIndex, out MagazineItemInstance item, out _, out ItemDrag drag))
            return false;
        
        // 드래그 중이거나 다른 작업이 사용하는 탄창은 선택하지 않습니다.
        if (drag == null || drag.IsDragging || item.IsReserved || !inventory.CanSupplyOneRoundTo(inventoryIndex, item))
            return false;
        
        // 이 컴포넌트 자체를 예약 소유자로 사용합니다.
        if (!item.TryReserve(this))
            return false;

        _inventory = inventory;
        _target = item;
        _targetIndex = inventoryIndex;

        _roundDuration = Mathf.Max( MinimumSecondsPerRound, secondsPerRound);
        _remainingSeconds = _roundDuration;
        StateChanged?.Invoke();
        return true;
    }

    private void Update()
    {
        if (!IsPacking)
            return;

        // 창 닫기, 데이터 소실, 예약 해제 등의 상황에서 정리합니다.
        if (!IsInventoryOpen || _inventory == null || !_inventory.isActiveAndEnabled || !_target.IsReservedBy(this) || !_inventory.CanSupplyOneRoundTo(_targetIndex, _target))
        {
            CancelPacking();
            return;
        }

        float deltaTime = Time.deltaTime;

        // 일시정지 상태에서는 진행하지 않습니다.
        if (deltaTime <= 0f)
            return;

        _remainingSeconds = Mathf.Max(0f,_remainingSeconds - deltaTime);

        if (_remainingSeconds > 0f)
            return;

        // 다음 한 발의 시간을 준비한 뒤 실제 탄약을 옮깁니다.
        _remainingSeconds = _roundDuration;

        bool loaded;

        _isTransferring = true;

        try
        {
            loaded = _inventory.TryLoadOneIntoReserved( _targetIndex ,_target, this);
        }
        finally
        {
            _isTransferring = false;
        }

        // 탄창 Changed 이벤트에서 작업이 취소됐을 수도 있습니다.
        if (!IsPacking)
            return;

        if (!loaded || _inventory == null || !_inventory.CanSupplyOneRoundTo(_targetIndex, _target))
        {
            CancelPacking();
        }
    }


    //탄창 예약 해제
    public void CancelPacking()
    {
        if (!IsPacking)
            return;

        MagazineItemInstance target = _target;

        // 외부 알림 전에 내부 상태와 예약을 정리합니다.
        _target = null;
        _inventory = null;
        _targetIndex = -1;
        _roundDuration = 0f;
        _remainingSeconds = 0f;

        target.ReleaseReservation(this);

        StateChanged?.Invoke();
    }

    private void OnDisable()
    {
        CancelPacking();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Start Packing First Magazine")]
    private void DebugStartPacking()
    {
        if (!Application.isPlaying)
            return;

        const int firstMagazineIndex = 0;

        bool started = TryStartPacking(firstMagazineIndex);

        Debug.Log($"탄창 채우기 시작: {started}", this);
    }

    [ContextMenu("Debug/Cancel Packing")]
    private void DebugCancelPacking()
    {
        if (!Application.isPlaying)
            return;

        CancelPacking();
    }
#endif
}