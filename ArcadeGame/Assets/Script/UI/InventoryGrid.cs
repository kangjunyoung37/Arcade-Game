using UnityEngine;

public class InventoryGrid : MonoBehaviour
{
    private const int BitsPerRow = 32;

    [Header("Grid Settings")]
    [Range(1, BitsPerRow)] public int gridWidth = 10;

    [Min(1)] public int gridHeight = 10;

    [Min(1f)] public float cellSize = 50f;

    [Min(0f)] public float spacing = 0.5f;
    public Transform itemContainer;
    // 1비트 = 한 칸의 점유 여부
    private uint[] _rowMasks;
    // 예약된 칸을 표시합니다. 실제 아이템 점유와 별도로 관리합니다.
    private uint[] _reservedRowMasks;

    // 각 칸을 어떤 작업자가 예약했는지 저장합니다.
    private object[,] _reservationOwners;
    // 실제로 어떤 아이템이 점유하는지 저장
    private ItemDrag[,] _items;

    // 초기화 당시 크기를 유지합니다.
    private int _columns;
    private int _rows;

    public RectTransform ContainerRect => itemContainer as RectTransform;

    private float Step => cellSize + spacing;

    private void Awake()
    {
        EnsureInitialized();
    }

    private bool EnsureInitialized()
    {
        if (_rowMasks != null)
            return ContainerRect != null;

        if (gridWidth < 1 || gridWidth > BitsPerRow || gridHeight < 1 || cellSize <= 0f || spacing < 0f || ContainerRect == null)
            return false;
        
        _columns = gridWidth;
        _rows = gridHeight;

        _rowMasks = new uint[_rows];
        _items = new ItemDrag[_columns, _rows];

        _reservedRowMasks = new uint[_rows];
        _reservationOwners = new object[_columns, _rows];   

        return true;
    }

    public Vector2 GetPositionFromIndex(int x, int y)
    {
        return new Vector2(x * Step, -y * Step);
    }

    public Vector2Int GetIndexFromPosition(Vector2 localPosition)
    {
        return new Vector2Int( Mathf.RoundToInt(localPosition.x / Step), Mathf.RoundToInt(-localPosition.y / Step));
    }

    public Vector2 GetItemSize(int width, int height)
    {
        return new Vector2( width * cellSize + Mathf.Max(0, width - 1) * spacing, height * cellSize + Mathf.Max(0, height - 1) * spacing);
    }

    private bool IsInside(int x, int y, int width, int height)
    {
        // 뺄셈 형태로 검사하여 x + width 오버플로도 피합니다.
        return width > 0 && height > 0 && width <= _columns && height <= _rows && x >= 0 && y >= 0 && x <= _columns - width && y <= _rows - height;
    }

   
    // width개의 연속된 비트를 x만큼 이동합니다.
    // 호출 전에 범위 검사를 완료해야 합니다.
    private static uint MakeMask(int x, int width)
    {
        // C#에서 1u << 32는 원하는 결과가 아니므로 별도 처리합니다.
        uint bits = width == BitsPerRow ? uint.MaxValue : (1u << width) - 1u;
        return bits << x;
    }

  
    // 이동하는 아이템 자신의 기존 영역은 충돌에서 제외합니다.
    // 실제 마스크는 변경하지 않습니다.
    public bool CanPlaceItem( int x, int y, int width, int height, ItemDrag movingItem = null)
    {
        if (!EnsureInitialized() ||
            !IsInside(x, y, width, height))
        {
            return false;
        }

        uint candidateMask = MakeMask(x, width);

        bool ignoreSelf = movingItem != null && movingItem.IsPlaced && movingItem._myGrid == this;

        uint selfMask = 0u;
        int selfStartY = 0;
        int selfEndY = 0;

        if (ignoreSelf)
        {
            if (!IsInside( movingItem.currentGridX,  movingItem.currentGridY, movingItem.width, movingItem.height))
                return false;
            

            selfMask = MakeMask( movingItem.currentGridX, movingItem.width);
            selfStartY = movingItem.currentGridY;
            selfEndY = selfStartY + movingItem.height;
        }

        for (int row = y; row < y + height; row++)
        {
            uint occupied = _rowMasks[row];

            // 이동 중인 아이템 자신의 기존 점유만 제외합니다.
            if (ignoreSelf && row >= selfStartY && row < selfEndY)
                occupied &= ~selfMask;

            // 예약 영역은 자신의 기존 점유와 별도로 차단합니다.
            uint blocked = occupied | _reservedRowMasks[row];

            if ((blocked & candidateMask) != 0u)
                return false;
        }
        return true;
    }

    
    //목적지 검사 후 기존 배치를 제거하고 새 배치를 등록합니다.
    //실패하면 기존 배치를 유지합니다.
   
    public bool TryPlaceItem( ItemDrag item, int x, int y, int width, int height)
    {
        if (item == null)
            return false;

        InventoryGrid source = item._myGrid;

        // 확정된 배치 정보와 실제 소유 칸이 맞는지 확인합니다.
        if (item.IsPlaced && (source == null || !source.OwnsPlacement(item)))
            return false;
        
        if (!CanPlaceItem(x, y, width, height, item))
            return false;

        // 이 아래에서는 검사 실패로 되돌아가지 않습니다.
        if (item.IsPlaced)
            source.ClearCommittedPlacement(item);

        uint mask = MakeMask(x, width);

        for (int row = y; row < y + height; row++)
        {
            _rowMasks[row] |= mask;

            for (int column = x; column < x + width; column++)
                _items[column, row] = item;
        }

        item.ApplyPlacement(this, x, y, width, height);
        return true;
    }

    private bool OwnsPlacement(ItemDrag item)
    {
        if (!EnsureInitialized() || !item.IsPlaced ||item._myGrid != this || !IsInside( item.currentGridX, item.currentGridY, item.width, item.height))
            return false;
        
        for (int y = item.currentGridY; y < item.currentGridY + item.height; y++)
        {
            for (int x = item.currentGridX; x < item.currentGridX + item.width; x++)
            {
                if (_items[x, y] != item)
                    return false;
            }
        }

        return true;
    }

   private void ClearCommittedPlacement(ItemDrag item)
   {
        uint mask = MakeMask(item.currentGridX, item.width);

        for (int y = item.currentGridY; y < item.currentGridY + item.height; y++)
        {
            _rowMasks[y] &= ~mask;
            for (int x = item.currentGridX; x < item.currentGridX + item.width; x++)
            {
                _items[x, y] = null;
            }
        }
   }

    
    // 빈 공간을 왼쪽 위부터 검색합니다.
    // 최초 배치에 사용하며 현재 배치는 변경하지 않습니다.
   public bool TryFindSpace(int width, int height, out Vector2Int position, ItemDrag movingItem = null)
   {
      position = default;

      if (!EnsureInitialized() || width <= 0 || height <= 0 || width > _columns || height > _rows)
         return false;
      

      for (int y = 0; y <= _rows - height; y++)
      {
         for (int x = 0; x <= _columns - width; x++)
         {
               if (!CanPlaceItem( x, y, width, height, movingItem))
                  continue;
               
               position = new Vector2Int(x, y);
               return true;
         }
      }

      return false;
   }

    public ItemDrag GetItemAt(int x, int y)
    {
        if (!EnsureInitialized() || x < 0 || y < 0 || x >= _columns || y >= _rows)
            return null;
        
        return _items[x, y];
    }
    /// <summary>
    /// 비어 있는 직사각형 영역을 예약합니다.
    /// 실제 아이템 배치와 점유 마스크는 변경하지 않습니다.
    /// </summary>
    public bool TryReserveArea(object owner, int x, int y, int width, int height)
    {
        if (owner == null)
            return false;

        // movingItem을 전달하지 않습니다.
        // 이번 API는 실제로 비어 있는 공간만 예약합니다.
        if (!CanPlaceItem(x, y, width, height))
            return false;

        uint mask = MakeMask(x, width);

        for (int row = y; row < y + height; row++)
        {
            _reservedRowMasks[row] |= mask;

            for (int column = x; column < x + width; column++)
            {
                _reservationOwners[column, row] = owner;
            }
        }

        return true;
    }
    /// <summary>
    /// 왼쪽 위부터 빈 공간을 검색한 뒤 예약합니다.
    /// 실패하면 기존 예약과 아이템 배치는 유지됩니다.
    /// </summary>
    public bool TryReserveSpace( object owner, int width, int height, out Vector2Int position)
    {
        position = default;

        if (owner == null)
            return false;

        if (!TryFindSpace(width, height, out Vector2Int candidate))
            return false;

        if (!TryReserveArea( owner, candidate.x,candidate.y, width, height))
        {
            return false;
        }

        position = candidate;
        return true;
    }
    /// <summary>
    /// 이 그리드에서 owner가 소유한 예약을 모두 해제합니다.
    /// 다른 작업자의 예약과 실제 아이템은 변경하지 않습니다.
    /// 반환값은 해제한 칸 수입니다.
    /// </summary>
    public int ReleaseReservations(object owner)
    {
        if (owner == null || _reservationOwners == null)
            return 0;

        int releasedCells = 0;

        for (int row = 0; row < _rows; row++)
        {
            // 예약이 없는 행은 검사하지 않습니다.
            if (_reservedRowMasks[row] == 0u)
                continue;

            for (int column = 0; column < _columns; column++)
            {
                if (!ReferenceEquals(_reservationOwners[column, row], owner))
                    continue;
                
                _reservationOwners[column, row] = null;
                _reservedRowMasks[row] &= ~(1u << column);

                releasedCells++;
            }
        }

        return releasedCells;
    }
    internal void ForgetDestroyedItem(ItemDrag item)
    {
        if (_items == null)
            return;

        // 파괴 시에는 실제 소유 배열을 기준으로 정리합니다.
        // 드래그/프레임 루프에서 실행되는 검색이 아닙니다.
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                if (!ReferenceEquals(_items[x, y], item))
                    continue;

                _items[x, y] = null;
                _rowMasks[y] &= ~(1u << x);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Validate Occupancy")]
    private void ValidateOccupancy()
    {
        if (!Application.isPlaying || !EnsureInitialized())
            return;

        for (int y = 0; y < _rows; y++)
        {
            uint expectedMask = 0u;

            for (int x = 0; x < _columns; x++)
            {
                if (_items[x, y] != null)
                    expectedMask |= 1u << x;
            }

            if (expectedMask != _rowMasks[y])
            {
                Debug.LogError(
                    $"점유 정보 불일치: {name}, 행 {y}",
                    this);
                return;
            }
        }

        Debug.Log("비트마스크와 아이템 배열이 일치합니다.", this);
    }
    private readonly object _debugReservationOwner = new object();
    private bool _debugHasReservation;

    [ContextMenu("Debug/Reserve Empty 1x2 Area")]
    private void DebugReserveArea()
    {
        if (!Application.isPlaying)
            return;

        if (_debugHasReservation)
        {
            Debug.Log(
                "이미 테스트 공간을 예약했습니다. 먼저 해제하세요.",
                this);
            return;
        }

        const int testWidth = 1;
        const int testHeight = 2;

        if (!TryReserveSpace(
                _debugReservationOwner,
                testWidth,
                testHeight,
                out Vector2Int position))
        {
            Debug.Log(
                "예약 실패: 비어 있는 1x2 공간이 없습니다.",
                this);
            return;
        }

        _debugHasReservation = true;

        Debug.Log(
            $"예약 성공: {name}, 시작 칸 ({position.x}, {position.y}), " +
            $"크기 {testWidth}x{testHeight}",
            this);
    }

    [ContextMenu("Debug/Release Test Reservation")]
    private void DebugReleaseReservation()
    {
        if (!Application.isPlaying)
            return;

        int releasedCells =
            ReleaseReservations(_debugReservationOwner);

        _debugHasReservation = false;

        Debug.Log(
            $"테스트 예약 해제: {releasedCells}칸",
            this);
    }
#endif
}