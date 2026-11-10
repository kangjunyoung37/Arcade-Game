using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazineInventoryPresenter : MonoBehaviour
{
    [SerializeField] private MagazineInventory inventory;
    [SerializeField] private MagazineItemView[] views;

    private void OnEnable()
    {
        Rebind();
    }

    /// <summary>
    /// 보관 목록의 인스턴스를 각 UI에 연결합니다.
    /// UI만 다시 연결하며 탄창은 생성하지 않습니다.
    /// </summary>
    public void Rebind()
    {
        if (inventory != null)
            inventory.EnsureInitialized();

        if (views == null)
            return;

        for (int i = 0; i < views.Length; i++)
        {
            MagazineItemView view = views[i];

            if (view == null)
                continue;

            MagazineItemInstance item = null;

            if (inventory != null)
                inventory.TryGetItem(i, out item);

            view.Bind(item);
        }
    }
}
