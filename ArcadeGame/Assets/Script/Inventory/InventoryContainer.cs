using UnityEngine;

public enum InventoryContainerKind
{
    Rig,
    Pockets,
    Backpack,
    SecureContainer
}

[DisallowMultipleComponent]
public sealed class InventoryContainer : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private InventoryContainerKind kind;

    [Header("Compartments — 검색 우선순위 순서")]
    [SerializeField] private InventoryGrid[] compartments;

    public InventoryContainerKind Kind => kind;

    // 현재 게임에서 적용할 빠른 재장전 정책입니다.
    // 가방은 나중에 마우스를 이용한 수동 교체를 허용합니다.
    public bool AllowsQuickReload => kind == InventoryContainerKind.Rig || kind == InventoryContainerKind.Pockets;

    /// <summary>
    /// 지정한 그리드가 이 보관함에 포함되는지 검사합니다.
    /// UI의 활성화 여부와는 관계없습니다.
    /// </summary>
    public bool ContainsGrid(InventoryGrid grid)
    {
        if (grid == null || compartments == null)
            return false;

        for (int i = 0; i < compartments.Length; i++)
        {
            if (compartments[i] == grid)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 아이템의 확정된 배치가 이 보관함에 있는지 검사합니다.
    /// 드래그 중에도 확정 전까지는 원래 보관함 소속입니다.
    /// </summary>
    public bool ContainsItem(ItemDrag item)
    {
        return item != null &&
               item.IsPlaced &&
               ContainsGrid(item._myGrid);
    }

    /// <summary>
    /// 여러 주머니를 순서대로 검사하여 빈자리를 찾습니다.
    /// 검색만 수행하며 실제 아이템 배치는 변경하지 않습니다.
    /// 이번 단계에서는 회전을 자동으로 시도하지 않습니다.
    /// </summary>
    public bool TryFindSpace( int width, int height, out InventoryGrid targetGrid, out Vector2Int position, ItemDrag movingItem = null)
    {
        targetGrid = null;
        position = default;

        if (width <= 0 || height <= 0 || compartments == null)
            return false;

        for (int i = 0; i < compartments.Length; i++)
        {
            InventoryGrid grid = compartments[i];

            if (grid == null)
                continue;

            if (!grid.TryFindSpace( width,height, out Vector2Int candidatePosition, movingItem))
            {
                continue;
            }

            targetGrid = grid;
            position = candidatePosition;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Find Space For 1x2 Item")]
    private void DebugFindSpaceForMagazine()
    {
        if (!Application.isPlaying)
            return;

        const int testWidth = 1;
        const int testHeight = 2;

        bool found = TryFindSpace(
            testWidth,
            testHeight,
            out InventoryGrid grid,
            out Vector2Int position);

        if (!found)
        {
            Debug.Log(
                $"{name}: 1x2 아이템이 들어갈 공간이 없습니다.",
                this);

            return;
        }

        Debug.Log(
            $"{name}: {grid.name}의 " +
            $"({position.x}, {position.y})에 배치할 수 있습니다.",
            grid);
    }
#endif
}