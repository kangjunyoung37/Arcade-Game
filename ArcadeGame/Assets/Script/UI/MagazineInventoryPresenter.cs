using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazineInventoryPresenter : MonoBehaviour
{
    [Serializable]
    private sealed class ItemBinding
    {
        public MagazineItemView view;
        public ItemDrag drag;
        public InventoryGrid initialGrid;
    }

    [SerializeField] private MagazineInventory inventory;
    [SerializeField] private ItemBinding[] bindings;

    public MagazineInventory Inventory => inventory;
    public int BindingCount => bindings != null ? bindings.Length : 0;
    private void OnEnable()
    {
        Rebind();
    }

    public void Rebind()
    {
        if (inventory == null || bindings == null)
            return;

        inventory.EnsureInitialized();

        for (int i = 0; i < bindings.Length; i++)
        {
            ItemBinding binding = bindings[i];

            if (binding == null || binding.view == null || binding.drag == null)
                continue;
            

            if (!inventory.TryGetItem( i, out MagazineItemInstance item))
            {
                binding.view.Bind(null);
                continue;
            }

            binding.view.Bind(item);
            
            // UI 표시와 드래그가 같은 탄창 인스턴스를 참조합니다.
            binding.drag.BindInteractionLock(item);

            bool placed = binding.drag.TryInitialize( binding.initialGrid, item.Data.GridWidth, item.Data.GridHeight);

            if (!placed)
            {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.LogError(
                                $"탄창 {i}의 배치에 실패했습니다. " +
                                "그리드 연결과 빈 공간을 확인하세요.",
                                binding.drag);
            #endif
            }
        }
    }
    /// <summary>
    /// 인벤토리 데이터와 UI가 같은 탄창을 참조하는지 확인하고 반환합니다.
    /// </summary>
    public bool TryGetBoundItem( int index, out MagazineItemInstance item, out MagazineItemView view, out ItemDrag drag)
    {
        item = null;
        view = null;
        drag = null;

        if (inventory == null || bindings == null || index < 0 || index >= bindings.Length)
            return false;
        
        ItemBinding binding = bindings[index];

        if (binding == null || binding.view == null || binding.drag == null || !inventory.TryGetItem(index, out MagazineItemInstance found))
            return false;

        if (!ReferenceEquals(binding.view.Item, found) || !binding.drag.IsBoundTo(found) || !binding.drag.IsPlaced)
            return false;
    
        item = found;
        view = binding.view;
        drag = binding.drag;

        return true;
    }
}