using UnityEngine;

public sealed class MagazineReloadReservation
{
    private MagazineItemInstance _incomingMagazine;

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

        _incomingMagazine = null;
        ReturnGrid = null;
        ReturnPosition = default;
        ReturnSize = default;
        IsPrepared = false;
    }
}