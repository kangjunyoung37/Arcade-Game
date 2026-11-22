using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class WeaponMagazineDropTarget : MonoBehaviour
{
    [SerializeField] private PlayerReloadService reloadService;

    /// <summary>
    /// 드래그 정리가 끝난 아이템을 재장전 대상으로 전달합니다.
    /// 실제 탄창 교환은 기존 무기 시스템이 처리합니다.
    /// </summary>
    public bool TryAcceptItem(ItemDrag item)
    {
        if (!isActiveAndEnabled ||
            reloadService == null ||
            item == null)
        {
            return false;
        }

        bool started = reloadService.TryStartManualReload(item);

#if UNITY_EDITOR
        Debug.Log(
            $"[탄창 드롭] 재장전 시작={started}",
            this);
#endif

        return started;
    }
}