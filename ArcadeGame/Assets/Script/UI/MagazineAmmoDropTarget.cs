using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MagazineItemView))]
public sealed class MagazineAmmoDropTarget : MonoBehaviour
{
    [SerializeField] private MagazineInventoryPresenter presenter;
    [SerializeField] private MagazinePackingController packingController;

    private MagazineItemView _view;

    private void Awake()
    {
        _view = GetComponent<MagazineItemView>();
    }

    public bool TryAcceptItem(ItemDrag incoming)
    {
        if (!isActiveAndEnabled ||presenter == null || packingController == null || _view == null || incoming == null || !incoming.isActiveAndEnabled || incoming.IsDragging || !incoming.IsPlaced)
            return false;
        
        // 탄창 등 다른 아이템은 받지 않습니다.
        LooseAmmoStack source = incoming.BoundItem as LooseAmmoStack;

        if (source == null || source.IsEmpty || source.IsReserved)
            return false;

        MagazineInventory inventory = presenter.Inventory;

        if (inventory == null)
            return false;

        // 전달받은 탄약이 실제로 이 인벤토리에 있는지 확인합니다.
        int ammoIndex = -1;

        for (int i = 0; i < inventory.AmmoStackCount; i++)
        {
            if (inventory.TryGetAmmoStack(i, out LooseAmmoStack stack) && ReferenceEquals(stack, source))
            {
                ammoIndex = i;
                break;
            }
        }

        if (ammoIndex < 0)
            return false;

        // 이 UI가 현재 표시하는 실제 탄창의 인덱스를 찾습니다.
        for (int i = 0; i < presenter.BindingCount; i++)
        {
            if (!presenter.TryGetBoundItem(i, out _, out MagazineItemView view, out _))
                continue;
            
            if (view != _view)
                continue;

            return packingController.TryStartPacking(i, ammoIndex);
        }

        return false;
    }
}