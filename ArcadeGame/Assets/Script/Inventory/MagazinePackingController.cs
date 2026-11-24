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
    private LooseAmmoStack _source;

    private int _targetIndex = -1;
    private int _sourceIndex = -1;

    private float _roundDuration;
    private float _remainingSeconds;
    private bool _isTransferring;

    public bool IsPacking => _target != null;

    public MagazineItemInstance TargetMagazine => _target;
    public LooseAmmoStack SourceAmmo => _source;

    public float CurrentRoundProgress => IsPacking && _roundDuration > 0f ? Mathf.Clamp01(1f - _remainingSeconds / _roundDuration) : 0f;

    public float CurrentRoundRemainingSeconds => IsPacking ? Mathf.Max(0f, _remainingSeconds) : 0f;

    public event Action StateChanged;

    private bool IsInventoryOpen => inventoryWindow != null && inventoryWindow.activeInHierarchy;

    
    // 선택한 탄약 묶음으로 선택한 탄창을 채웁니다.
    // 시작할 때는 탄약을 소비하지 않습니다.
  
    public bool TryStartPacking( int magazineIndex, int ammoStackIndex)
    {
        if (!isActiveAndEnabled || IsPacking || _isTransferring || !IsInventoryOpen || Time.timeScale <= 0f || presenter == null)
            return false;

        MagazineInventory inventory = presenter.Inventory;

        if (inventory == null || !inventory.isActiveAndEnabled)
            return false;

        if (!presenter.TryGetBoundItem(magazineIndex, out MagazineItemInstance magazine, out _, out ItemDrag drag))
            return false;
        

        if (!inventory.TryGetAmmoStack(ammoStackIndex, out LooseAmmoStack source))
        {
            return false;
        }

        if (drag == null || drag.IsDragging || magazine.IsReserved || source.IsReserved || !inventory.CanSupplyOneRoundTo(magazineIndex, magazine, ammoStackIndex, source))
            return false;

        // 두 아이템을 모두 예약해야 작업을 시작합니다.
        if (!magazine.TryReserve(this))
            return false;

        if (!source.TryReserve(this))
        {
            // 두 번째 예약이 실패하면 첫 번째 예약도 되돌립니다.
            magazine.ReleaseReservation(this);
            return false;
        }

        _inventory = inventory;
        _target = magazine;
        _source = source;
        _targetIndex = magazineIndex;
        _sourceIndex = ammoStackIndex;

        _roundDuration = Mathf.Max(MinimumSecondsPerRound, secondsPerRound);

        _remainingSeconds = _roundDuration;

        StateChanged?.Invoke();
        return true;
    }


    // 작업 중에도 두 아이템을 다시 검사합니다.
    private bool CanContinuePacking()
    {
        return IsPacking && IsInventoryOpen &&
               _inventory != null &&
               _inventory.isActiveAndEnabled &&
               _source != null &&
               _target.IsReservedBy(this) &&
               _source.IsReservedBy(this) &&
               _inventory.CanSupplyOneRoundTo(_targetIndex, _target,_sourceIndex, _source);
    }

    private void Update()
    {
        if (!IsPacking)
            return;

        if (!CanContinuePacking())
        {
            CancelPacking();
            return;
        }

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - deltaTime);

        if (_remainingSeconds > 0f)
            return;

        _remainingSeconds = _roundDuration;

        bool loaded;
        _isTransferring = true;

        try
        {
            loaded = _inventory.TryLoadOneIntoReserved(_targetIndex, _target, _sourceIndex, _source, this);
        }
        catch
        {
            // UI 이벤트 등에 예외가 생겨도 예약은 남기지 않습니다.
            CancelPacking();
            throw;
        }
        finally
        {
            _isTransferring = false;
        }

        // 데이터 변경 이벤트에서 취소했을 수도 있습니다.
        if (!IsPacking)
            return;

        if (!loaded || !CanContinuePacking())
            CancelPacking();
    }


    // 완료, 취소 시 두 아이템의 예약을 모두 해제합니다.
    // 이미 옮긴 탄약은 그대로 유지합니다.
    public void CancelPacking()
    {
        if (!IsPacking)
            return;

        MagazineItemInstance target = _target;
        LooseAmmoStack source = _source;

        _target = null;
        _source = null;
        _inventory = null;

        _targetIndex = -1;
        _sourceIndex = -1;
        _roundDuration = 0f;
        _remainingSeconds = 0f;

        target.ReleaseReservation(this);

        if (source != null)
            source.ReleaseReservation(this);

        StateChanged?.Invoke();
    }

    private void OnDisable()
    {
        CancelPacking();
    }

#if UNITY_EDITOR
    [Header("Debug Selection")]
    [SerializeField, Min(0)] private int debugMagazineIndex;
    [SerializeField, Min(0)] private int debugAmmoStackIndex;

    [ContextMenu("Debug/Start Packing Selected Ammo")]
    private void DebugStartPacking()
    {
        if (!Application.isPlaying)
            return;

        bool started = TryStartPacking(
            debugMagazineIndex,
            debugAmmoStackIndex);

        Debug.Log(
            $"채우기 시작={started}, " +
            $"탄창={debugMagazineIndex}, " +
            $"탄약 묶음={debugAmmoStackIndex}",
            this);
    }

    [ContextMenu("Debug/Cancel Packing")]
    private void DebugCancelPacking()
    {
        if (Application.isPlaying)
            CancelPacking();
    }

    [ContextMenu("Debug/Print Ammo Stacks")]
    private void DebugPrintAmmoStacks()
    {
        if (!Application.isPlaying ||
            presenter == null ||
            presenter.Inventory == null)
        {
            return;
        }

        MagazineInventory inventory = presenter.Inventory;

        for (int i = 0; i < inventory.AmmoStackCount; i++)
        {
            if (!inventory.TryGetAmmoStack(
                    i,
                    out LooseAmmoStack stack))
            {
                continue;
            }

            string ammoName = stack.Ammo != null
                ? stack.Ammo.bulletName
                : "(Missing)";

            Debug.Log(
                $"묶음 {i}: {ammoName}, " +
                $"{stack.Count}발, 예약={stack.IsReserved}",
                this);
        }
    }
#endif
}