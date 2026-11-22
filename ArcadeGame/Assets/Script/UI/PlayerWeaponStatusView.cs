using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerWeaponStatusView : MonoBehaviour
{
    private const int TimeDisplayStepsPerSecond = 10;
    private const float FullCycleRadians = Mathf.PI * 2f;
    private const string NoWeaponText = "무기 없음";
    private const string NoMagazineText = "탄창 없음";
    private const string EmptyCountText = "-";

    [Header("Player Source")]
    [SerializeField] private GameManager gameManager;

    [Header("Weapon")]
    [SerializeField] private TMP_Text weaponNameText;

    [Header("Reload")]
    [SerializeField] private GameObject reloadRoot;
    [SerializeField] private Image reloadFill;
    
    [Header("Visibility")]
    [SerializeField] private UIController inventoryUI;
    [SerializeField] private bool hideReloadWhenInventoryOpen;

    [Header("Remaining Time")]
    [SerializeField] private TMP_Text reloadRemainingText;

    [Header("Reload Blink")]
    [SerializeField] private CanvasGroup reloadBlinkTarget;
    [SerializeField, Range(0.1f, 1f)] private float minimumBlinkAlpha = 0.35f;
    [SerializeField, Min(0.1f)] private float blinkCyclesPerSecond = 1.5f;

    private PlayerWeapon _weapon;
    private bool _lastUsableWeapon;
    private int _lastRemainingTenths = -1;
    private float _blinkBaseAlpha = 1f;

    private void Awake()
    {
        if (reloadBlinkTarget != null)
            _blinkBaseAlpha = reloadBlinkTarget.alpha;
    }

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
        
        RestoreReloadBlink();
        if (!ReferenceEquals(inventoryUI, null))
            inventoryUI.InventoryOpenStateChanged -= HandleInventoryVisibility;
        if (!ReferenceEquals(gameManager, null))
            gameManager.CharacterChanged -= HandleCharacterChanged;

        UnsubscribeWeapon();
        _weapon = null;
    }

    private void RefreshReloadBlink()
    {
        if (reloadBlinkTarget == null || _weapon == null)
            return;

        float phase = _weapon.ReloadElapsedSeconds * blinkCyclesPerSecond * FullCycleRadians;
        // 밝음 → 어두움 → 밝음으로 부드럽게 반복합니다.
        float wave = (Mathf.Cos(phase) + 1f) * 0.5f;

        float alpha = _blinkBaseAlpha * Mathf.Lerp(minimumBlinkAlpha, 1f, wave);

        if (!Mathf.Approximately(reloadBlinkTarget.alpha, alpha))
            reloadBlinkTarget.alpha = alpha;
    }

    private void RestoreReloadBlink()
    {
        if (reloadBlinkTarget == null)
            return;

        if (!Mathf.Approximately(reloadBlinkTarget.alpha, _blinkBaseAlpha))
        {
            reloadBlinkTarget.alpha = _blinkBaseAlpha;
        }
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
        bool usable = _weapon != null && _weapon.isActiveAndEnabled;

        if (usable != _lastUsableWeapon)
        {
            RefreshState();
            return;
        }

        bool hiddenByInventory = hideReloadWhenInventoryOpen && inventoryUI != null && inventoryUI.IsInventoryOpen;
        bool showReload = usable && _weapon.IsReloading && !hiddenByInventory;

        if (!showReload)
        {
            RestoreReloadBlink();
            return;
        }

        RefreshReloadBlink();

        if (reloadRoot != null && reloadRoot.activeInHierarchy)
            RefreshReloadProgress();
    }

    private void RefreshState()
    {
        bool usable = _weapon != null && _weapon.isActiveAndEnabled;

        _lastUsableWeapon = usable;
        WeaponData weaponData = usable ? _weapon.Data : null;

        if (weaponNameText != null)
        {
            string displayName = weaponData != null
                ? weaponData.weaponName
                : NoWeaponText;

            weaponNameText.SetText(displayName);
        }
        bool hiddenByInventory = hideReloadWhenInventoryOpen && inventoryUI != null && inventoryUI.IsInventoryOpen;

        bool reloading = usable && _weapon.IsReloading && !hiddenByInventory;

        if (reloadRoot != null && reloadRoot.activeSelf != reloading)
        {
            reloadRoot.SetActive(reloading);
        }

        _lastRemainingTenths = -1;

        if (reloading)
        {
            RefreshReloadProgress();
            RefreshReloadBlink();
        }
        else
        {
            RestoreReloadBlink();

            if (reloadFill != null)
                reloadFill.fillAmount = 0f;
                
            if (reloadRemainingText != null)
                reloadRemainingText.SetText(string.Empty);
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

    }
    private void HandleInventoryVisibility(bool isOpen)
    {
        RefreshState();
    }
}