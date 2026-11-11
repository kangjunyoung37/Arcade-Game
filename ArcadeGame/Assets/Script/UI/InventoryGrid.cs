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

            if (ignoreSelf && row >= selfStartY && row < selfEndY)
                occupied &= ~selfMask;

            if ((occupied & candidateMask) != 0u)
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
#endif
}