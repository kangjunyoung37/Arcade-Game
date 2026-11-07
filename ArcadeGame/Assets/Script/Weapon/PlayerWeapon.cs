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
    private AmmoMagazine _equippedMagazine;
    private AmmoChamber _chamber;
    private WeaponData _initializedWeapon;

    private float _lastFireTime = float.NegativeInfinity;

    public WeaponData Data => weaponData;
    public float CurrentSpread { get; private set; }
    public bool HasMagazine => _equippedMagazine != null;

    public int MagazineCount => _equippedMagazine != null ? _equippedMagazine.Count : 0;

    public int ChamberCount => _chamber != null && _chamber.HasLiveRound ? 1 : 0;

    public int TotalLoadedRounds => MagazineCount + ChamberCount;

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
        if (!CanOperateAmmo || Time.deltaTime <= 0f || muzzle == null || _pool == null || !_pool.IsReady || GameManager.Instance == null)
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
        _chamber.TryFeedFromMagazine(_equippedMagazine);

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
    /// 외부 보관 슬롯과 무기의 탄창을 맞바꿉니다
    ///
    /// 성공:
    /// - incomingSlot의 탄창이 무기에 장착됩니다
    /// - 기존 장착 탄창은 incomingSlot으로 돌아갑니다
    ///
    /// incomingSlot이 null이면 장착된 탄창을 제거합니다
    /// 실패하면 양쪽 상태를 변경하지 않습니다
    /// </summary>
    public bool TryExchangeMagazine(ref AmmoMagazine incomingSlot)
    {
        if (!CanOperateAmmo)
            return false;

        // 같은 탄창을 다시 넣거나 양쪽 모두 비어 있으면 변경하지 않습니다
        if (ReferenceEquals(incomingSlot, _equippedMagazine))
            return false;

        if (incomingSlot != null && incomingSlot.Caliber != _chamber.Caliber)
            return false;
        

        AmmoMagazine previousMagazine = _equippedMagazine;

        _equippedMagazine = incomingSlot;
        incomingSlot = previousMagazine;

        unchecked
        {
            MagazineRevision++;
        }

        // 탄창을 교체했다고 자동으로 탄약을 추가하지 않습니다
        return true;
    }

  
    //약실이 비어 있을 때 장착된 탄창에서 한 발을 공급
    //이미 실탄이 있으면 그대로 유지하고 false를 반환
    public bool TryChamberRound()
    {
        if (!CanOperateAmmo || !_chamber.IsEmpty || _equippedMagazine == null)
            return false;
        

        return _chamber.TryFeedFromMagazine(_equippedMagazine);
    }
}