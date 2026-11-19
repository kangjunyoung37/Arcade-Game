using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerWeaponStatusView : MonoBehaviour
{
    private const string MagazineCountFormat = "{0}/{1}";
    private const string ChamberCountFormat = "약실 {0}";
    private const string ReloadPercentFormat = "{0}%";

    private const string NoWeaponText = "무기 없음";
    private const string NoMagazineText = "탄창 없음";
    private const string EmptyCountText = "-";
    private const int PercentScale = 100;

    [Header("Player Source")]
    [SerializeField] private GameManager gameManager;

    [Header("Magazine")]
    [SerializeField] private Image magazineIcon;
    [SerializeField] private TMP_Text magazineNameText;
    [SerializeField] private TMP_Text magazineCountText;
    [SerializeField] private TMP_Text chamberCountText;

    [Header("Reload")]
    [SerializeField] private GameObject reloadRoot;
    [SerializeField] private Image reloadFill;
    [SerializeField] private TMP_Text reloadPercentText;
    
    [Header("Visibility")]
    [SerializeField] private UIController inventoryUI;
    [SerializeField] private bool hideReloadWhenInventoryOpen;

    private PlayerWeapon _weapon;
    private bool _lastUsableWeapon;
    private int _lastReloadPercent = -1;

    private void OnEnable()
    { 
        if (inventoryUI != null)
            inventoryUI.InventoryOpenStateChanged += HandleInventoryVisibility;
        if (gameManager == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(
                "PlayerWeaponStatusView의 GameManager를 연결하세요.",
                this);
#endif

            BindWeapon(null);
            return;
        }

        gameManager.CharacterChanged += HandleCharacterChanged;

        // UI가 닫혀 있는 동안 생성된 플레이어도 연결합니다.
        HandleCharacterChanged(gameManager.GetCharacter());
    }

    private void OnDisable()
    {

        if (!ReferenceEquals(inventoryUI, null))
            inventoryUI.InventoryOpenStateChanged -= HandleInventoryVisibility;
        if (!ReferenceEquals(gameManager, null))
            gameManager.CharacterChanged -= HandleCharacterChanged;

        UnsubscribeWeapon();
        _weapon = null;
    }

    private void HandleCharacterChanged(Character player)
    {
        PlayerWeapon weapon = null;

        // 플레이어가 바뀌거나 UI를 열 때만 조회합니다.
        if (player != null)
            player.TryGetComponent(out weapon);

        BindWeapon(weapon);
    }

    private void BindWeapon(PlayerWeapon weapon)
    {
        UnsubscribeWeapon();

        _weapon = weapon;

        if (_weapon != null)
            _weapon.StateChanged += RefreshState;

        RefreshState();
    }

    private void UnsubscribeWeapon()
    {
        // Unity 오브젝트가 파괴됐어도 관리 객체의 이벤트는 해제합니다.
        if (!ReferenceEquals(_weapon, null))
            _weapon.StateChanged -= RefreshState;
    }

    private void LateUpdate()
    {
        bool usable =
            _weapon != null && _weapon.isActiveAndEnabled;

        // 무기 컴포넌트가 제거되는 상황도 화면에 반영합니다.
        if (usable != _lastUsableWeapon)
        {
            RefreshState();
            return;
        }

        if (usable && _weapon.IsReloading)
            RefreshReloadProgress();
    }

    private void RefreshState()
    {
        bool usable = _weapon != null && _weapon.isActiveAndEnabled;

        _lastUsableWeapon = usable;

        MagazineItemInstance magazine = usable ? _weapon.EquippedMagazine : null;

        MagazineData data = magazine != null ? magazine.Data : null;

        if (magazineIcon != null)
        {
            Sprite sprite = data != null ? data.Icon : null;

            if (magazineIcon.sprite != sprite)
                magazineIcon.sprite = sprite;

            bool showIcon = sprite != null;

            if (magazineIcon.enabled != showIcon)
                magazineIcon.enabled = showIcon;
        }

        if (magazineNameText != null)
        {
            string name = !usable ? NoWeaponText : data != null ? data.DisplayName : NoMagazineText;

            magazineNameText.SetText(name);
        }

        if (magazineCountText != null)
        {
            if (magazine != null)
            {
                magazineCountText.SetText( MagazineCountFormat, magazine.RoundCount, magazine.Capacity);
            }
            else
            {
                magazineCountText.SetText(EmptyCountText);
            }
        }

        if (chamberCountText != null)
        {
            chamberCountText.SetText(ChamberCountFormat, usable ? _weapon.ChamberCount : 0);
        }

        bool hiddenByInventory = hideReloadWhenInventoryOpen && inventoryUI != null && inventoryUI.IsInventoryOpen;

        bool reloading = usable && _weapon.IsReloading && !hiddenByInventory;

        if (reloadRoot != null && reloadRoot.activeSelf != reloading)
        {
            reloadRoot.SetActive(reloading);
        }

        _lastReloadPercent = -1;

        if (reloading)
        {
            RefreshReloadProgress();
        }
        else
        {
            if (reloadFill != null)
                reloadFill.fillAmount = 0f;

            if (reloadPercentText != null)
                reloadPercentText.SetText(string.Empty);
        }
    }

    private void RefreshReloadProgress()
    {
        if (_weapon == null)
            return;

        float progress = Mathf.Clamp01(_weapon.ReloadProgress);

        if (reloadFill != null && !Mathf.Approximately(reloadFill.fillAmount, progress))
        {
            reloadFill.fillAmount = progress;
        }

        int percent = Mathf.FloorToInt(progress * PercentScale);

        // 퍼센트 숫자가 바뀌었을 때만 텍스트를 갱신합니다.
        if (percent == _lastReloadPercent)
            return;

        _lastReloadPercent = percent;

        if (reloadPercentText != null)
            reloadPercentText.SetText(ReloadPercentFormat, percent);
    }
    private void HandleInventoryVisibility(bool isOpen)
    {
        RefreshState();
    }
}