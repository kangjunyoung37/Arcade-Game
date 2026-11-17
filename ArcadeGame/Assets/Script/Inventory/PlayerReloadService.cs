using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerReloadService : MonoBehaviour
{
    [SerializeField] private MagazineInventoryPresenter presenter;

    [Tooltip("빠른 재장전에 사용할 리그와 주머니를 연결합니다.")]
    [SerializeField] private InventoryContainer[] quickReloadContainers;

    private void Start()
    {
        if (presenter == null || presenter.Inventory == null)
        {
            ReportMissingReferences();
            enabled = false;
            return;
        }

        // 인벤토리 창을 한 번도 열지 않았어도 초기 배치를 준비합니다.
        // 비활성 UI를 켜지는 않습니다.
        presenter.Rebind();
    }

    public bool TryStartQuickReload(PlayerWeapon weapon)
    {
        if (!isActiveAndEnabled ||
            presenter == null ||
            presenter.Inventory == null ||
            quickReloadContainers == null ||
            weapon == null ||
            !weapon.isActiveAndEnabled ||
            weapon.Data == null ||
            weapon.IsReloading ||
            Time.timeScale <= 0f)
        {
            return false;
        }

        // 이전에 시도한 후보보다 우선순위가 낮은 후보를 찾습니다.
        int previousRounds = int.MaxValue;
        int previousIndex = -1;

        while (true)
        {
            int bestIndex = -1;
            int bestRounds = -1;

            MagazineItemView bestView = null;
            ItemDrag bestDrag = null;
            InventoryContainer bestContainer = null;

            for (int i = 0; i < presenter.BindingCount; i++)
            {
                if (!presenter.TryGetBoundItem(i, out MagazineItemInstance item, out MagazineItemView view, out ItemDrag drag))           
                    continue;
                

                if (item.IsEmpty || item.IsReserved || drag.IsDragging || ReferenceEquals(item, weapon.EquippedMagazine) || !weapon.Data.IsMagazineCompatible(item.Data))
                    continue;
                
                InventoryContainer container = FindQuickReloadContainer(drag._myGrid);

                if (container == null)
                    continue;

                int rounds = item.RoundCount;

                // 이미 시도한 후보와 그보다 높은 우선순위를 제외합니다.
                if (rounds > previousRounds || (rounds == previousRounds && i <= previousIndex))
                    continue;
                
                // 같은 잔탄 수이면 먼저 발견한 인덱스를 유지합니다.
                if (rounds <= bestRounds)
                    continue;

                bestIndex = i;
                bestRounds = rounds;
                bestView = view;
                bestDrag = drag;
                bestContainer = container;
            }

            if (bestIndex < 0)
                return false;

            // 먼저 선택 탄창이 있던 보관함으로 반환을 시도합니다.
            if (weapon.TryStartInventoryReload( presenter.Inventory, bestIndex, bestView, bestDrag, bestContainer))
                return true;
            
            // 같은 보관함에 반환할 수 없으면 다른 리그/주머니를 시도합니다.
            for (int i = 0; i < quickReloadContainers.Length; i++)
            {
                InventoryContainer container = quickReloadContainers[i];

                if (container == null || container == bestContainer || !container.AllowsQuickReload)
                    continue;
                

                if (weapon.TryStartInventoryReload( presenter.Inventory, bestIndex, bestView, bestDrag, container))
                    return true;
                
            }

            // 이 후보로는 준비할 수 없으므로 다음 우선순위로 넘어갑니다.
            previousRounds = bestRounds;
            previousIndex = bestIndex;
        }
    }

    private InventoryContainer FindQuickReloadContainer(InventoryGrid grid)
    {
        if (grid == null || quickReloadContainers == null)
            return null;

        for (int i = 0; i < quickReloadContainers.Length; i++)
        {
            InventoryContainer container = quickReloadContainers[i];

            if (container != null && container.AllowsQuickReload && container.ContainsGrid(grid))
                return container;
        }

        return null;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportMissingReferences()
    {
        Debug.LogError(
            "PlayerReloadService의 Presenter와 Inventory 연결을 확인하세요.",
            this);
    }
}