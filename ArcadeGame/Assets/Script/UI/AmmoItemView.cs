using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(ItemDrag))]
public sealed class AmmoItemView : MonoBehaviour
{
    private const int GridWidth = 1;
    private const int GridHeight = 1;
    private const string CountFormat = "{0}";

    [Header("Source")]
    [SerializeField] private MagazineInventory inventory;
    [SerializeField, Min(0)] private int ammoStackIndex;

    [Header("Initial Placement")]
    [SerializeField] private InventoryGrid initialGrid;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    private ItemDrag _drag;
    private LooseAmmoStack _stack;

    private void Awake()
    {
        _drag = GetComponent<ItemDrag>();

        // 표시용 자식들은 클릭을 받지 않습니다.
        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (nameText != null)
            nameText.raycastTarget = false;

        if (countText != null)
            countText.raycastTarget = false;
    }

    private void OnEnable()
    {
        Unsubscribe();

        if (inventory == null || initialGrid == null)
        {
            ReportConfigurationError();
            enabled = false;
            return;
        }

        inventory.EnsureInitialized();

        if (!inventory.TryGetAmmoStack(ammoStackIndex, out _stack))
        {
            ReportConfigurationError();
            enabled = false;
            return;
        }

        // 탄약이 예약되면 기존 ItemDrag가 이동을 막습니다.
        _drag.BindInteractionLock(_stack);

        if (!_drag.TryInitialize(initialGrid, GridWidth, GridHeight))
        {
            ReportConfigurationError();
            enabled = false;
            return;
        }

        _stack.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (_stack != null)
            _stack.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (_stack == null)
            return;

        BulletData data = _stack.Ammo;

        if (iconImage != null)
        {
            Sprite icon = data != null ? data.inventoryIcon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
        {
            nameText.SetText(data != null ? data.bulletName : string.Empty);
        }

        if (countText != null)
            countText.SetText(CountFormat, _stack.Count);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportConfigurationError()
    {
        Debug.LogError(
            "AmmoItemView의 Inventory, Ammo Stack Index, " +
            "Initial Grid와 그리드의 빈 공간을 확인하세요.",
            this);
    }
}