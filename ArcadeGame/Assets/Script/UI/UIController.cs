using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class UIController : MonoBehaviour
{
    private const KeyCode DefaultInventoryToggleKey = KeyCode.Tab;

    [Header("Inventory")]
    [Tooltip("활성화/비활성화할 인벤토리 UI 패널입니다.")]
    [SerializeField] private GameObject inventoryWindow;
    [SerializeField] private KeyCode inventoryToggleKey = DefaultInventoryToggleKey;
    [SerializeField] private bool openInventoryOnStart;

    [Header("Cursor")]
    [SerializeField] private bool manageCursorVisibility = true;

    public bool IsInventoryOpen { get; private set; }

    public event Action<bool> InventoryOpenStateChanged;

    private void Awake()
    {

        SetInventoryOpen(openInventoryOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryToggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        SetInventoryOpen(!IsInventoryOpen);
    }

    public void OpenInventory()
    {
        SetInventoryOpen(true);
    }

    public void CloseInventory()
    {
        SetInventoryOpen(false);
    }

    public void SetInventoryOpen(bool isOpen)
    {
        if (inventoryWindow == null)
        {
            Debug.LogWarning("인벤토리 UI 패널이 할당되지 않았습니다. 인벤토리 UI를 열거나 닫을 수 없습니다.");
            return;
        }

        bool stateChanged = IsInventoryOpen != isOpen;
        IsInventoryOpen = isOpen;

        if (inventoryWindow.activeSelf != isOpen)
        {
            inventoryWindow.SetActive(isOpen);
        }

        if (manageCursorVisibility)
        {
            Cursor.visible = isOpen;
        }

        if (stateChanged)
        {
            InventoryOpenStateChanged?.Invoke(isOpen);
        }
    }


}
