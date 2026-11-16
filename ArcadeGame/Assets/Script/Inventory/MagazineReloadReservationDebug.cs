using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazineReloadReservationDebug : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private PlayerWeapon weapon;
    [SerializeField] private MagazineInventory inventory;

    [SerializeField, Min(0)] private int inventoryIndex;

    [SerializeField] private MagazineItemView incomingView;
    [SerializeField] private ItemDrag incomingDrag;
    [SerializeField] private InventoryContainer returnContainer;

    [ContextMenu("Debug/Start Inventory Reload")]
    private void DebugStartReload()
    {
        if (!Application.isPlaying ||
            weapon == null ||
            inventory == null)
        {
            return;
        }

        bool started = weapon.TryStartInventoryReload(
            inventory,
            inventoryIndex,
            incomingView,
            incomingDrag,
            returnContainer);

        Debug.Log(
            $"인벤토리 재장전 시작={started}, " +
            $"현재 장착 ID={weapon.EquippedMagazine?.InstanceId}, " +
            $"선택 탄창 ID={incomingView?.Item?.InstanceId}",
            this);
    }

    [ContextMenu("Debug/Cancel Inventory Reload")]
    private void DebugCancelReload()
    {
        if (!Application.isPlaying || weapon == null)
            return;

        weapon.CancelReload();

        Debug.Log("인벤토리 재장전 취소.", this);
    }

    [ContextMenu("Debug/Print Inventory Reload State")]
    private void DebugPrintState()
    {
        if (!Application.isPlaying ||
            weapon == null ||
            inventory == null)
        {
            return;
        }

        inventory.TryGetItem(
            inventoryIndex,
            out MagazineItemInstance stored);

        Debug.Log(
            $"재장전 중={weapon.IsReloading}, " +
            $"장착 ID={weapon.EquippedMagazine?.InstanceId}, " +
            $"인벤토리 ID={stored?.InstanceId}, " +
            $"화면 ID={incomingView?.Item?.InstanceId}, " +
            $"탄창={weapon.MagazineCount}, " +
            $"약실={weapon.ChamberCount}",
            this);
    }
#endif
}