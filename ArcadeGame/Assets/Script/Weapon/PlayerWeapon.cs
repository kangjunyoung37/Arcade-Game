using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeapon : MonoBehaviour
{
    private const string BulletPoolKey = "Bullet";
    private const string CasingPoolKey = "Casing";
    private const float SpreadRecoveryDelayMultiplier = 1.5f;

    [Header("Weapon")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform muzzle;

    [Header("Casing")]
    [Tooltip("비워 두면 총구 위치에서 배출합니다.")]
    [SerializeField] private Transform casingEjectPoint;

    private ObjectPoolManager _pool;
    private WeaponData _trackedWeapon;
    private MagazineSlot _reloadSlot;
    private MagazineItemInstance _equippedMagazine;
    private AmmoChamber _chamber;
    private WeaponData _initializedWeapon;

    private float _lastFireTime = float.NegativeInfinity;
    private float _reloadDuration;
    private float _remainingReloadTime;
    public WeaponData Data => weaponData;
    public float CurrentSpread { get; private set; }
    public float ReloadProgress => IsReloading && _reloadDuration > 0f ? Mathf.Clamp01(1f - _remainingReloadTime / _reloadDuration) : 0f;
    public bool HasMagazine => _equippedMagazine != null;
    public int MagazineCount => _equippedMagazine != null ? _equippedMagazine.RoundCount : 0;
    // 장착된 탄창의 ID와 데이터를 확인할 때 사용합니다.
    public MagazineItemInstance EquippedMagazine => _equippedMagazine;  

    public int ChamberCount => _chamber != null && _chamber.HasLiveRound ? 1 : 0;

    public int TotalLoadedRounds => MagazineCount + ChamberCount;

    public bool IsReloading => _reloadSlot != null;

    //탄창이 바뀌었음을 입력 컨트롤러가 감지할 때 사용합니다.
    public uint MagazineRevision { get; private set; }

    private bool CanOperateAmmo => isActiveAndEnabled && weaponData != null && weaponData == _initializedWeapon && _chamber != null && weaponData.caliber == _chamber.Caliber && weaponData.actionType == WeaponActionType.SelfLoading && weaponData.reloadType == WeaponReloadType.MagazineSwap;

    // 발사 위치, 이번 발사에 사용한 탄퍼짐
    public event Action<Vector3, float> Fired;

    private void Awake()
    {
        SynchronizeWeaponData();
        if (weaponData == null || weaponData.caliber == null || weaponData.actionType != WeaponActionType.SelfLoading || weaponData.reloadType != WeaponReloadType.MagazineSwap)
        {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("구경이 설정된 SelfLoading / MagazineSwap 무기가 필요합니다.", this);
        #endif
            enabled = false;
            return;
        }

        _initializedWeapon = weaponData;
        _chamber = new AmmoChamber(weaponData.caliber);
    }

    private void Start()
    {
        _pool = ObjectPoolManager.instance;
    }

    private void Update()
    {
        SynchronizeWeaponData();
        RecoverSpread();
    }


    //무기 데이터가 변경되면 새 무기의 기본 탄퍼짐을 적용합니다.
    private void SynchronizeWeaponData()
    {
        if (_trackedWeapon == weaponData)
            return;
        _trackedWeapon = weaponData;
        CurrentSpread = weaponData != null ? Mathf.Max(0f, weaponData.baseSpread) : 0f;
    }


    //마지막 발사 이후 일정 시간이 지나면 탄퍼짐을 회복합니다.
    private void RecoverSpread()
    {
        if (weaponData == null || Time.deltaTime <= 0f)
            return;

        float recoveryDelay = weaponData.GetShotInterval(weaponData.fireMode) * SpreadRecoveryDelayMultiplier;

        if (Time.time < _lastFireTime + recoveryDelay)
            return;

        float baseSpread = Mathf.Max(0f, weaponData.baseSpread);
        float maximumSpread = Mathf.Max(baseSpread, weaponData.maxSpread);

        CurrentSpread = Mathf.MoveTowards( Mathf.Clamp(CurrentSpread, baseSpread, maximumSpread), baseSpread, Mathf.Max(0f, weaponData.recoveryRate) * Time.deltaTime);
    }


    // 실제 총알 한 발을 발사합니다.
    public bool TryFire()
    {
        if (!CanOperateAmmo || IsReloading || Time.deltaTime <= 0f || muzzle == null || _pool == null || !_pool.IsReady || GameManager.Instance == null)
            return false;
        
        // 탄창이 없어도 약실에 실탄이 있으면 발사할 수 있습니다.
        if (!_chamber.TryPeekLoadedRound(out BulletData ammo))
            return false;

        // 이번 단계는 탄약 한 발당 투사체 하나만 지원합니다.
        if (!weaponData.IsAmmoCompatible(ammo) || ammo.speed <= 0f || ammo.projectilesPerRound != 1)
            return false;
        
        // 풀에서 가져오지 못했다면 약실의 탄약은 그대로 유지됩니다.
        Bullet bullet = _pool.GetBullet(BulletPoolKey);

        if (bullet == null)
            return false;

        if (!_chamber.TryConsumeRound(out BulletData firedAmmo))
        {
            bullet.ReturnToPool();
            return false;
        }

        float baseSpread = Mathf.Max(0f, weaponData.baseSpread);
        float maximumSpread = Mathf.Max(baseSpread, weaponData.maxSpread);

        float shotSpread = Mathf.Clamp(CurrentSpread, baseSpread, maximumSpread);

        float randomYaw = UnityEngine.Random.Range( -shotSpread, shotSpread);

        Vector3 shotPosition = muzzle.position;

        Quaternion shotRotation = muzzle.rotation * Quaternion.Euler(0f, randomYaw, 0f);

        bullet.transform.SetPositionAndRotation( shotPosition, shotRotation );

        // 풀에서 재사용한 총알에도 실제 소비한 탄약을 매번 적용합니다.
        bullet.bulletData = firedAmmo;
        bullet.speed = firedAmmo.speed;
        bullet.Init();

        _lastFireTime = Time.time;

        CurrentSpread = Mathf.Clamp( shotSpread + Mathf.Max(0f, weaponData.bloomPerShot), baseSpread, maximumSpread);

        //탄피 배출 후 다음 실탄 공급.
        bool ejected = _chamber.TryEject(out _);

        // 탄창이 없거나 비었다면 약실은 빈 상태로 남습니다.
        TryFeedChamberFromEquippedMagazine();

        if (ejected)
            EjectCasing();

        Fired?.Invoke(shotPosition, shotSpread);
        return true;
    }

 
    // 탄피 연출을 실행합니다
    private void EjectCasing()
    {
        if (_pool == null)
            return;

        Transform ejectPoint = casingEjectPoint != null ? casingEjectPoint : muzzle;

        if (ejectPoint == null)
            return;

        Casing casing = _pool.GetCasing(CasingPoolKey);

        if (casing == null)
            return;

        casing.transform.SetPositionAndRotation( ejectPoint.position, ejectPoint.rotation);

        Vector3 ejectDirection = (ejectPoint.right + ejectPoint.up).normalized;
        casing.Eject(ejectDirection);
    }
    /// <summary>
    /// 외부 슬롯의 탄창과 장착 탄창을 교환합니다.
    /// incomingSlot이 null이면 장착 탄창을 제거합니다.
    /// 약실의 탄약은 변경하지 않습니다.
    /// </summary>
    public bool TryExchangeMagazine(
        ref MagazineItemInstance incomingSlot)
    {
        if (!CanOperateAmmo || IsReloading)
            return false;

        if (ReferenceEquals(incomingSlot, _equippedMagazine))
            return false;

        // null은 탄창 제거 요청이므로 허용합니다.
        if (incomingSlot != null && !IsCompatibleMagazine(incomingSlot))
        {
            return false;
        }

        MagazineItemInstance previousMagazine = _equippedMagazine;

        _equippedMagazine = incomingSlot;
        incomingSlot = previousMagazine;

        unchecked
        {
            MagazineRevision++;
        }

        return true;
    }

  
    //약실이 비어 있을 때 장착된 탄창에서 한 발을 공급
    //이미 실탄이 있으면 그대로 유지하고 false를 반환
    public bool TryChamberRound()
    {
        if (!CanOperateAmmo || IsReloading || !_chamber.IsEmpty || _equippedMagazine == null)
            return false;
        
        return TryFeedChamberFromEquippedMagazine();
    }
    /// <summary>
    /// 지정한 슬롯의 탄창으로 재장전을 시작합니다.
    /// 이 시점에는 탄창이나 약실을 변경하지 않습니다.
    /// </summary>
    public bool TryStartReload(MagazineSlot sourceSlot)
    {
        if (!CanOperateAmmo || IsReloading || Time.timeScale <= 0f || sourceSlot == null || !sourceSlot.HasMagazine)
        {
            return false;
        }

        MagazineItemInstance incomingMagazine = sourceSlot.Magazine;

        if (ReferenceEquals(incomingMagazine, _equippedMagazine) || !IsCompatibleMagazine(incomingMagazine))
        {
            return false;
        }

        if (!sourceSlot.TryReserve(this))
            return false;

        _reloadSlot = sourceSlot;

        _reloadDuration = Mathf.Max( WeaponData.MinimumShotInterval, weaponData.reloadTime);

        _remainingReloadTime = _reloadDuration;
        return true;
    }


    // 재장전 시간을 진행합니다.
    public void TickReload(float deltaTime)
    {
        if (!IsReloading)
            return;

        if (!CanOperateAmmo)
        {
            CancelReload();
            return;
        }

        if (deltaTime <= 0f)
            return;

        _remainingReloadTime = Mathf.Max(0f, _remainingReloadTime - deltaTime);

        if (_remainingReloadTime > 0f)
            return;

        CompleteReload();
    }


    // 완료 시점에 탄창 교환을 한 번만 적용합니다.
    private void CompleteReload()
    {
        MagazineSlot slot = _reloadSlot;

        if (slot == null)
            return;

        MagazineItemInstance incomingMagazine = slot.Magazine;

        if (!CanOperateAmmo || !IsCompatibleMagazine(incomingMagazine) || ReferenceEquals(incomingMagazine, _equippedMagazine))
        {
            CancelReload();
            return;
        }

        if (!slot.TryExchangeReserved(this, ref _equippedMagazine))
        {
            CancelReload();
            return;
        }

        // 기존 약실 탄약은 유지합니다.
        // 비어 있을 때만 새 탄창에서 한 발을 옮깁니다.
        if (_chamber.IsEmpty)
            TryFeedChamberFromEquippedMagazine();

        unchecked
        {
            MagazineRevision++;
        }

        CancelReload();
    }


    // 탄창을 변경하지 않고 예약과 진행 상태만 해제합니다.
    // 완료 전에는 실제 교환을 하지 않았으므로 되돌릴 탄창도 없습니다
    public void CancelReload()
    {
        if (_reloadSlot != null)
            _reloadSlot.ReleaseReservation(this);

        _reloadSlot = null;
        _reloadDuration = 0f;
        _remainingReloadTime = 0f;
    }
    
    private void OnDisable()
    {
        CancelReload();
    }
    // 탄약 구경뿐 아니라 탄창의 결합 규격도 검사합니다.
    private bool IsCompatibleMagazine(MagazineItemInstance magazine)
    {
        return magazine != null && weaponData != null && weaponData.IsMagazineCompatible(magazine.Data);
    }

    // 장착 탄창에서 약실로 한 발을 공급합니다.
    // 재장전 완료 처리에서도 사용하므로 IsReloading 검사는 하지 않습니다.
    private bool TryFeedChamberFromEquippedMagazine()
    {
        return _equippedMagazine != null && _equippedMagazine.TryFeedIntoChamber(_chamber);
    }
}