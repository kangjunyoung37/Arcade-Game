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
}