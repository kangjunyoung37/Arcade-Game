using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerWeapon))]
public sealed class PlayerMagazineDebug : MonoBehaviour
{
#if UNITY_EDITOR
    private const int TestCapacity = 5;
    private const int InitialRoundsA = 3;
    private const int InitialRoundsB = 2;

    [SerializeField] private BulletData testAmmo;

    private PlayerWeapon _weapon;

    // 테스트용 인벤토리 슬롯 두 개입니다.
    private AmmoMagazine _spareMagazine;
    private AmmoMagazine _removedMagazine;

    private bool _ready;

    private void Awake()
    {
        _weapon = GetComponent<PlayerWeapon>();
    }

    private void Start()
    {
        if (_weapon == null ||
            !_weapon.isActiveAndEnabled ||
            _weapon.Data == null ||
            testAmmo == null ||
            !_weapon.Data.IsAmmoCompatible(testAmmo) ||
            testAmmo.speed <= 0f ||
            testAmmo.projectilesPerRound != 1)
        {
            Debug.LogError("테스트 무기와 탄약 설정을 확인해 주세요.", this);
            return;
        }

        if (_weapon.HasMagazine || _weapon.ChamberCount != 0)
        {
            Debug.LogError("빈 무기에서 테스트를 시작해 주세요.", this);
            return;
        }

        // 시작 장비 지급을 흉내 내는 테스트 초기화입니다.
        LooseAmmoStack supply = new LooseAmmoStack(
            testAmmo,
            InitialRoundsA + InitialRoundsB);

        _spareMagazine = new AmmoMagazine(
            testAmmo.caliber,
            TestCapacity);

        for (int i = 0; i < InitialRoundsA; i++)
            supply.TryLoadOneInto(_spareMagazine);

        // A를 무기에 넣습니다.
        // 기존 장착 탄창이 없으므로 성공 후 spare 슬롯은 null입니다.
        if (!_weapon.TryExchangeMagazine(ref _spareMagazine))
        {
            Debug.LogError("초기 탄창 장착에 실패했습니다.", this);
            return;
        }

        // A의 실탄 한 발을 약실로 옮깁니다.
        if (!_weapon.TryChamberRound())
        {
            Debug.LogError("초기 약실 장전에 실패했습니다.", this);
            return;
        }

        // 예비 슬롯에는 B를 준비합니다.
        _spareMagazine = new AmmoMagazine(
            testAmmo.caliber,
            TestCapacity);

        for (int i = 0; i < InitialRoundsB; i++)
            supply.TryLoadOneInto(_spareMagazine);

        _ready = true;
        PrintState();
    }

    [ContextMenu("Debug/Exchange Spare Magazine")]
    private void ExchangeSpareMagazine()
    {
        if (!Application.isPlaying || !_ready)
            return;

        if (_spareMagazine == null)
        {
            Debug.LogWarning("예비 슬롯에 탄창이 없습니다.", this);
            return;
        }

        bool changed =
            _weapon.TryExchangeMagazine(ref _spareMagazine);

        Debug.Log($"탄창 교체 성공: {changed}", this);
        PrintState();
    }

    [ContextMenu("Debug/Remove Magazine")]
    private void RemoveMagazine()
    {
        if (!Application.isPlaying || !_ready)
            return;

        if (_removedMagazine != null)
        {
            Debug.LogWarning("제거 탄창 보관 슬롯이 이미 차 있습니다.", this);
            return;
        }

        bool changed =
            _weapon.TryExchangeMagazine(ref _removedMagazine);

        Debug.Log($"탄창 제거 성공: {changed}", this);
        PrintState();
    }

    [ContextMenu("Debug/Reinsert Removed Magazine")]
    private void ReinsertRemovedMagazine()
    {
        if (!Application.isPlaying || !_ready)
            return;

        if (_removedMagazine == null || _weapon.HasMagazine)
        {
            Debug.LogWarning(
                "무기의 탄창 슬롯이 비어 있고, 제거한 탄창이 있어야 합니다.",
                this);
            return;
        }

        bool changed =
            _weapon.TryExchangeMagazine(ref _removedMagazine);

        Debug.Log($"탄창 재장착 성공: {changed}", this);
        PrintState();
    }

    [ContextMenu("Debug/Chamber One Round")]
    private void ChamberOneRound()
    {
        if (!Application.isPlaying || !_ready)
            return;

        bool chambered = _weapon.TryChamberRound();

        Debug.Log($"약실 장전 성공: {chambered}", this);
        PrintState();
    }

    [ContextMenu("Debug/Print Ammo State")]
    private void PrintState()
    {
        if (!Application.isPlaying || !_ready)
            return;

        int spareCount =
            _spareMagazine != null ? _spareMagazine.Count : 0;

        int removedCount =
            _removedMagazine != null ? _removedMagazine.Count : 0;

        int total =
            _weapon.TotalLoadedRounds + spareCount + removedCount;

        Debug.Log(
            $"탄창 장착: {_weapon.HasMagazine}, " +
            $"장착 탄창: {_weapon.MagazineCount}, " +
            $"약실: {_weapon.ChamberCount}, " +
            $"예비 탄창: {spareCount}, " +
            $"제거 탄창: {removedCount}, " +
            $"총 실탄: {total}",
            this);
    }
#endif
}