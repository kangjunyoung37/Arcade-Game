using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MagazineInventory : MonoBehaviour
{
    [Serializable]
    private sealed class StartingMagazine
    {
        public MagazineData data;
        public BulletData ammo;

        [Min(0)]
        public int rounds;
    }

    [Header("Starting Magazines")]
    [SerializeField] private StartingMagazine[] startingMagazines;

    [Header("Loose Ammunition")]
    [SerializeField] private BulletData looseAmmoData;

    [SerializeField, Min(0)] private int startingLooseRounds = 10;

    private MagazineItemInstance[] _items;
    private LooseAmmoStack _looseAmmo;
    private bool _initialized;

    public int ItemCount => _items != null ? _items.Length : 0;

    public int LooseRoundCount => _looseAmmo != null ? _looseAmmo.Count : 0;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 시작 장비를 한 번만 생성합니다.
    /// UI에서 먼저 호출해도 중복 생성하지 않습니다.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        int count = startingMagazines != null ? startingMagazines.Length : 0;

        _items = new MagazineItemInstance[count];

        // 시작 장비 생성 시에만 객체와 탄약 배열을 할당합니다.
        for (int i = 0; i < count; i++)
        {
            StartingMagazine entry = startingMagazines[i];

            if (entry == null || entry.data == null || !entry.data.IsValid || entry.rounds < 0 || entry.rounds > entry.data.Capacity)
            {
                ReportInvalidEntry(i);
                continue;
            }

            if (entry.rounds > 0 && (entry.ammo == null || entry.ammo.caliber != entry.data.Caliber))
            {
                ReportInvalidEntry(i);
                continue;
            }

            MagazineItemInstance item = new MagazineItemInstance(entry.data);

            if (entry.rounds > 0)
            {
                // 시작 지급량을 탄창으로 옮깁니다.
                LooseAmmoStack initialSupply = new LooseAmmoStack(entry.ammo, entry.rounds);

                for (int round = 0; round < entry.rounds; round++)
                    item.TryLoadOneFrom(initialSupply);
            }

            _items[i] = item;
        }

        if (looseAmmoData != null && startingLooseRounds >= 0)
        {
            _looseAmmo = new LooseAmmoStack( looseAmmoData,startingLooseRounds );
        }
    }

    /// <summary>
    /// 지정한 위치의 실제 탄창 인스턴스를 반환합니다.
    /// 복사하거나 새로 생성하지 않습니다.
    /// </summary>
    public bool TryGetItem(int index, out MagazineItemInstance item)
    {
        item = null;

        if (_items == null || index < 0 || index >= _items.Length)
            return false;

        item = _items[index];
        return item != null;
    }

    /// <summary>
    /// 보유한 낱개 탄약에서 지정한 탄창으로 한 발 이동합니다.
    /// 현재는 데이터/UI 검증용 즉시 처리입니다.
    /// </summary>
    public bool TryLoadOneInto(int index)
    {
        if (_looseAmmo == null ||
            !TryGetItem(index, out MagazineItemInstance item))
        {
            return false;
        }

        return item.TryLoadOneFrom(_looseAmmo);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportInvalidEntry(int index)
    {
        Debug.LogError( $"시작 탄창 설정 오류: 인덱스 {index}. " + "탄창 설정, 탄약 구경, 지급 수량을 확인하세요.", this);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Load One Into First Magazine")]
    private void DebugLoadOne()
    {
        if (!Application.isPlaying)
            return;

        bool loaded = TryLoadOneInto(0);

        Debug.Log(
            $"한 발 삽입: {loaded}, 남은 낱개 탄약: {LooseRoundCount}",
            this);
    }

    [ContextMenu("Debug/Print Magazine Identities")]
    private void DebugPrintIdentities()
    {
        if (!Application.isPlaying)
            return;

        for (int i = 0; i < ItemCount; i++)
        {
            if (!TryGetItem(i, out MagazineItemInstance item))
                continue;

            Debug.Log(
                $"슬롯 {i}, ID: {item.InstanceId}, " +
                $"잔탄: {item.RoundCount}/{item.Capacity}",
                this);
        }
    }

    private readonly object _debugItemReservationOwner = new object();
    private MagazineItemInstance _debugReservedMagazine;

    [ContextMenu("Debug/Reserve First Magazine")]
    private void DebugReserveFirstMagazine()
    {
        if (!Application.isPlaying)
            return;

        if (_debugReservedMagazine != null)
        {
            Debug.Log("먼저 기존 테스트 예약을 해제하세요.", this);
            return;
        }

        EnsureInitialized();

        const int firstMagazineIndex = 0;

        if (!TryGetItem(firstMagazineIndex, out MagazineItemInstance item))
        {
            Debug.LogWarning("첫 번째 탄창이 없습니다.", this);
            return;
        }

        if (!item.TryReserve(_debugItemReservationOwner))
        {
            Debug.LogWarning("이미 다른 작업이 예약한 탄창입니다.", this);
            return;
        }

        _debugReservedMagazine = item;

        Debug.Log(
            $"탄창 예약 성공: {item.InstanceId}",
            this);
    }

    [ContextMenu("Debug/Release First Magazine Reservation")]
    private void DebugReleaseFirstMagazineReservation()
    {
        if (!Application.isPlaying || _debugReservedMagazine == null)
            return;

        bool released = _debugReservedMagazine.ReleaseReservation(
            _debugItemReservationOwner);

        _debugReservedMagazine = null;

        Debug.Log($"탄창 예약 해제: {released}", this);
    }

    // 테스트용 예약자가 파괴될 때 자신이 만든 예약을 정리합니다.
    // OnDisable에서는 해제하지 않습니다.
    private void OnDestroy()
    {
        if (_debugReservedMagazine != null)
        {
            _debugReservedMagazine.ReleaseReservation(
                _debugItemReservationOwner);

            _debugReservedMagazine = null;
        }
    }
#endif
}