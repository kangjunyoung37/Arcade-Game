using UnityEngine;

public sealed class MagazineReloadReservation
{
    private MagazineItemInstance _incomingMagazine;
    private MagazineInventory _inventory;
    private int _inventoryIndex = -1;

    private MagazineItemView _incomingView;
    private ItemDrag _incomingDrag;

    private MagazineItemInstance _returningMagazine;
    private InventoryContainer _returnContainer;

    private InventoryGrid _sourceGrid;
    private Vector2Int _sourcePosition;
    private Vector2Int _sourceSize;
    public bool IsPrepared { get; private set; }

    public InventoryGrid ReturnGrid { get; private set; }
    public Vector2Int ReturnPosition { get; private set; }
    public Vector2Int ReturnSize { get; private set; }

    /// <summary>
    /// 탄창과 반환 공간을 함께 예약합니다.
    /// 실패 시 이번 호출에서 확보한 예약은 모두 해제합니다.
    ///
    /// returningMagazineData가 null이면:
    /// 기존 장착 탄창이 없으므로 반환 공간을 예약하지 않습니다.
    /// </summary>
    public bool TryPrepare( MagazineItemView incomingView, ItemDrag incomingDrag, MagazineData returningMagazineData, InventoryContainer returnContainer)
    {
        // 진행 중인 준비 작업을 덮어쓰지 않습니다.
        if (IsPrepared)
            return false;

        if (incomingView == null || incomingDrag == null || incomingView.gameObject != incomingDrag.gameObject)
            return false;
        
        MagazineItemInstance incoming = incomingView.Item;

        if (incoming == null || !incomingDrag.IsBoundTo(incoming) || !incomingDrag.IsPlaced || incomingDrag.IsDragging)
            return false;
        
        InventoryGrid sourceGrid = incomingDrag._myGrid;

        if (sourceGrid == null || !sourceGrid.OwnsPlacement(incomingDrag))
            return false;
        
        bool needsReturnSpace = returningMagazineData != null;

        if (needsReturnSpace && (!returningMagazineData.IsValid || returnContainer == null))
            return false;
        

        // 이 작업 객체 자체를 예약자로 사용합니다.
        if (!incoming.TryReserve(this))
            return false;

        _incomingMagazine = incoming;

        if (!needsReturnSpace)
        {
            IsPrepared = true;
            return true;
        }

        int width = returningMagazineData.GridWidth;
        int height = returningMagazineData.GridHeight;

        // 우선 교체할 탄창이 있던 자리를 사용합니다.
        // 단, 지정한 반환 보관함에 속한 경우에만 사용합니다.
        if (returnContainer.ContainsGrid(sourceGrid) && TryReserveAt( sourceGrid, incomingDrag, incomingDrag.currentGridX, incomingDrag.currentGridY, width, height))
        {
            IsPrepared = true;
            return true;
        }

        // 원래 자리에 들어가지 않으면 반환 보관함 전체를 검색
        // 같은 그리드에서는 outgoing 아이템의 점유만 제외
        if (returnContainer.TryFindSpace( width, height, out InventoryGrid targetGrid, out Vector2Int position, incomingDrag) &&
            TryReserveAt( targetGrid, incomingDrag, position.x, position.y, width, height))
        {
            IsPrepared = true;
            return true;
        }

        // 공간을 구하지 못했다면 앞서 잡은 탄창 예약도 해제합니다.
        Cancel();
        return false;
    }

    private bool TryReserveAt( InventoryGrid grid, ItemDrag outgoingItem, int x, int y, int width, int height)
    {
        if (grid == null)
            return false;

        bool reserved;

        if (grid == outgoingItem._myGrid)
        {
            reserved = grid.TryReserveReplacementArea( this, outgoingItem, x, y, width, height);
        }
        else
        {
            reserved = grid.TryReserveArea( this, x, y, width, height);
        }

        if (!reserved)
            return false;

        ReturnGrid = grid;
        ReturnPosition = new Vector2Int(x, y);
        ReturnSize = new Vector2Int(width, height);

        return true;
    }
    internal bool TryPrepareForWeapon( PlayerWeapon weapon, MagazineInventory inventory, int inventoryIndex, MagazineItemView view, ItemDrag drag, InventoryContainer returnContainer)
    {
        if (IsPrepared || weapon == null || inventory == null || view == null || drag == null)
            return false;
        
        inventory.EnsureInitialized();

        if (!inventory.TryGetItem(inventoryIndex, out MagazineItemInstance incoming) || !ReferenceEquals(incoming, view.Item))
            return false;
        
        MagazineItemInstance returning = weapon.EquippedMagazine;

        if (ReferenceEquals(incoming, returning))
            return false;

        MagazineData returningData = returning != null ? returning.Data : null;

        // 기존에 테스트한 탄창 + 반환 공간 예약을 재사용합니다.
        if (!TryPrepare(view, drag, returningData, returnContainer))
            return false;

        // 기존 장착 탄창의 탄약 변경도 잠급니다.
        if (returning != null && !returning.TryReserve(this))
        {
            Cancel();
            return false;
        }

        _returningMagazine = returning;
        _inventory = inventory;
        _inventoryIndex = inventoryIndex;

        _incomingView = view;
        _incomingDrag = drag;
        _returnContainer = returnContainer;

        _sourceGrid = drag._myGrid;
        _sourcePosition = new Vector2Int( drag.currentGridX, drag.currentGridY);

        _sourceSize = new Vector2Int(drag.width, drag.height);

        if (!inventory.CanReplaceReservedItem( inventoryIndex, incoming, returning, this))
        {
            Cancel();
            return false;
        }

        return true;
    }
    
    internal bool TryCommit(PlayerWeapon weapon)
    {
        if (!IsPrepared || weapon == null || _inventory == null || _incomingView == null || _incomingDrag == null || _sourceGrid == null)
            return false;
        
        // 준비 당시의 탄창과 배치가 그대로인지 확인합니다.
        if (!ReferenceEquals(_incomingView.Item, _incomingMagazine) ||
            !_incomingDrag.IsBoundTo(_incomingMagazine) ||
            !_incomingDrag.IsReservedBy(this) ||
            _incomingDrag.IsDragging ||
            _incomingDrag._myGrid != _sourceGrid ||
            _incomingDrag.currentGridX != _sourcePosition.x ||
            _incomingDrag.currentGridY != _sourcePosition.y ||
            _incomingDrag.width != _sourceSize.x ||
            _incomingDrag.height != _sourceSize.y ||
            !_sourceGrid.OwnsPlacement(_incomingDrag))
        {
            return false;
        }

        if (!_inventory.CanReplaceReservedItem( _inventoryIndex, _incomingMagazine, _returningMagazine, this))
            return false;
        

        if (_returningMagazine != null)
        {
            if (ReturnGrid == null ||
                _returnContainer == null ||
                !_returnContainer.ContainsGrid(ReturnGrid) ||
                ReturnSize.x != _returningMagazine.GridWidth ||
                ReturnSize.y != _returningMagazine.GridHeight ||
                !ReturnGrid.CanCommitReservedPlacement(
                    this,
                    _incomingDrag,
                    ReturnPosition.x,
                    ReturnPosition.y,
                    ReturnSize.x,
                    ReturnSize.y))
            {
                return false;
            }
        }

        // 무기가 마지막으로 자기 상태를 검증하고 탄창 참조를 바꿉니다.
        // 실패하면 아직 인벤토리와 그리드는 변경되지 않았습니다.
        if (!weapon.TryCommitInventoryMagazine(this,_returningMagazine,_incomingMagazine))
        {
            return false;
        }

        // 여기부터는 검증된 데이터 변경만 수행합니다.
        // 중간에 이벤트·UI 갱신·비동기 대기를 넣지 않습니다.
        MagazineItemInstance returning = _returningMagazine;
        MagazineItemView view = _incomingView;
        ItemDrag drag = _incomingDrag;

        _inventory.ReplaceItemAfterValidation(_inventoryIndex, returning);

        if (returning != null)
        {
            ReturnGrid.CommitReservedPlacement( drag, ReturnPosition.x, ReturnPosition.y, ReturnSize.x, ReturnSize.y);
        }
        else
        {
            _sourceGrid.RemovePlacementForReload(drag);
        }

        // 실제 데이터와 그리드 변경이 끝났으므로 예약을 해제합니다.
        Cancel();

        // 같은 UI 오브젝트로 반환된 탄창을 표시합니다.
        view.Bind(returning);
        drag.BindInteractionLock(returning);
        drag.RefreshPlacementVisual();

        return true;
    }
    /// <summary>
    /// 이 작업이 소유한 예약만 해제합니다.
    /// 아이템 위치·탄약·무기 상태는 변경하지 않습니다.
    /// 여러 번 호출해도 안전합니다.
    /// </summary>
  public void Cancel()
    {
        if (ReturnGrid != null)
            ReturnGrid.ReleaseReservations(this);

        if (_incomingMagazine != null)
            _incomingMagazine.ReleaseReservation(this);

        if (_returningMagazine != null)
            _returningMagazine.ReleaseReservation(this);

        _incomingMagazine = null;
        _returningMagazine = null;

        _inventory = null;
        _inventoryIndex = -1;

        _incomingView = null;
        _incomingDrag = null;

        _returnContainer = null;
        _sourceGrid = null;
        _sourcePosition = default;
        _sourceSize = default;

        ReturnGrid = null;
        ReturnPosition = default;
        ReturnSize = default;

        IsPrepared = false;
    }
}