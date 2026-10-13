using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyWeapon : MonoBehaviour
{
    private const string DefaultBulletPoolKey = "EnemyBullet";
    private const string DefaultCasingPoolKey = "Casing";
    private const float DirectionSqrEpsilon = 0.0001f;
    [Header("References")]
    [SerializeField] private Transform muzzle;

    [Header("Weapon")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private BulletData reserveAmmo;

    [Header("Casing")]
    [SerializeField] private Transform casingEjectPoint;
    [SerializeField] private string casingPoolKey = DefaultCasingPoolKey;

    [Header("Projectile")]
    [SerializeField] private string bulletPoolKey = DefaultBulletPoolKey;
    private ObjectPoolManager _pool;
    private InfiniteReserveAmmo _ammoSupply;
    private EnemyController _owner;
    //한 번 사격하는데 필요한 발사체를 저장
    private Bullet[] _reservedProjectiles;

    private BulletData _configuredAmmo;
    private float _remainingSpreadRecoveryDelay;

    public WeaponData Data => weaponData;
    public bool IsReloading => _ammoSupply != null && _ammoSupply.IsReloading;
    public bool IsCycling => _ammoSupply != null && _ammoSupply.IsCycling;
    public int LoadedRounds => _ammoSupply != null ? _ammoSupply.TotalLoadedRounds : 0;

    public bool CanFire => isActiveAndEnabled && _ammoSupply != null && _ammoSupply.CanFire;
    public float CurrentSpread{get; private set;}


    private void Awake()
    {
        if(muzzle == null || weaponData == null || reserveAmmo == null || string.IsNullOrEmpty(bulletPoolKey))
        {
            ReportConfigurationError("총구·무기·탄약·풀 키를 확인해 주세요.");
            enabled = false;
            return;
        }
        if (!weaponData.IsAmmoCompatible(reserveAmmo))
        {
            ReportConfigurationError("무기와 예비 탄약의 규격이 다릅니다.");
            enabled = false;
            return;
        }
        if(weaponData.magazineSize <= 0 || reserveAmmo.speed <= 0f || reserveAmmo.lifetime <= 0f)
        {
            ReportConfigurationError("탄창 용량, 탄속, 탄약 수명은 양수여야 합니다.");
            enabled = false;
            return;
        }

        bool supportedAction = weaponData.actionType == WeaponActionType.SelfLoading || weaponData.actionType == WeaponActionType.BoltAction || weaponData.actionType == WeaponActionType.PumpAction;
        bool supportedReload = weaponData.reloadType == WeaponReloadType.MagazineSwap || weaponData.reloadType == WeaponReloadType.InsertOneRound;

        if(!supportedAction || !supportedReload)
        {
            ReportConfigurationError("이번 구현은 자동 작동식·탄창 교체형·단일 발사체만 지원합니다. ");
            enabled = false;
            return;
        }
        if(reserveAmmo.projectilesPerRound < 1)
        {
            ReportConfigurationError("탄약 한 발의 발사체 수는 1이상이어여 합니다.");
            enabled = false;
            return;
        }
        _configuredAmmo = reserveAmmo;
        _reservedProjectiles = new Bullet[reserveAmmo.projectilesPerRound];
        CurrentSpread = Mathf.Clamp(weaponData.baseSpread, 0f, ProjectileSpread.MaximumHalfAngle);
        _ammoSupply = new InfiniteReserveAmmo(weaponData.caliber, weaponData.magazineSize, reserveAmmo, weaponData.reloadTime, weaponData.actionType, weaponData.actionCycleTime, weaponData.reloadType, weaponData.insertRoundTime);
        _ammoSupply.SpentCaseEjected += HandleSpentCaseEjected;

        _owner = GetComponent<EnemyController>();
    }

    private void Start()
    {
        _pool = ObjectPoolManager.instance;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        _ammoSupply?.Tick(deltaTime);
        RecoverSpread(deltaTime);
    }

    public bool TryFire(Vector3 aimPosition)
    {
        if (!CanFire || muzzle == null || weaponData == null ||_pool == null || !_pool.IsReady || GameManager.Instance == null)
        {
            return false;
        }
        if(_configuredAmmo == null || reserveAmmo != _configuredAmmo || _configuredAmmo.speed <= 0f || _configuredAmmo.lifetime <= 0f)
            return false;
        int projectileCount = _configuredAmmo.projectilesPerRound;

        if(_reservedProjectiles == null || projectileCount < 1 || projectileCount > _reservedProjectiles.Length)
            return false;
        
        Vector3 spawnPosition = muzzle.position;
        Vector3 direction = aimPosition - spawnPosition;

        if (direction.sqrMagnitude <= DirectionSqrEpsilon)
        {
            return false;
        }
        if (!TryReserveProjectiles(projectileCount))
        {
            return false;
        }
        if(!_ammoSupply.TryConsumeRound(out BulletData ammo))
        {
            ReturnReservedProjectiles(projectileCount);
            return false;
        }
        //탄퍼짐 설정
        Quaternion aimRotation = Quaternion.LookRotation(direction);
        Vector3 shotDirection = aimRotation * ProjectileSpread.SampleLocalDirection(CurrentSpread);
        Quaternion shotRotation = Quaternion.LookRotation(shotDirection);

        
        for(int i = 0; i < projectileCount; i++)
        {
            Bullet bullet = _reservedProjectiles[i];
            _reservedProjectiles[i] = null;

            Vector3 projectileDirection = projectileCount == 1 ? shotDirection : shotRotation * ProjectileSpread.SampleLocalDirection(ammo.pelletSpreadHalfAngle);
            bullet.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(projectileDirection));

            bullet.bulletData = ammo;
            bullet.speed = ammo.speed;
            bullet.Init();    
        }
        float baseSpread = Mathf.Clamp(weaponData.baseSpread , 0f, ProjectileSpread.MaximumHalfAngle);
        float maximumSpread = Mathf.Clamp(weaponData.maxSpread, baseSpread,ProjectileSpread.MaximumHalfAngle);
        CurrentSpread = Mathf.Clamp(CurrentSpread + Mathf.Max(0f, weaponData.bloomPerShot), baseSpread, maximumSpread);

        _remainingSpreadRecoveryDelay = weaponData.GetShotInterval(weaponData.fireMode);

        return true;
    }
    private bool TryReserveProjectiles(int count)
    {
        for(int i = 0; i < count; i++)
        {
            Bullet bullet = _pool.GetBullet(bulletPoolKey);
            if(bullet == null)
            {
                ReturnReservedProjectiles(i);
                return false;
            }
            _reservedProjectiles[i] = bullet;
        }
        return true;
    }

    private void ReturnReservedProjectiles(int count)
    {
        for(int i = 0; i < count; i++)
        {
            Bullet bullet = _reservedProjectiles[i];
            _reservedProjectiles[i] = null;
            if(bullet == null) continue;
            if(bullet.Pool != null) bullet.Pool.Release(bullet.gameObject);
            else bullet.gameObject.SetActive(false);
        }    
    }

    private void RecoverSpread(float deltaTime)
    {
        if(weaponData == null || deltaTime <= 0f) return;
        float recoveryTime = deltaTime;
        if(_remainingSpreadRecoveryDelay > 0f)
        {
            _remainingSpreadRecoveryDelay -= deltaTime;
            recoveryTime = Mathf.Max(0f, -_remainingSpreadRecoveryDelay);
        }
        float baseSpread = Mathf.Clamp(weaponData.baseSpread, 0f, ProjectileSpread.MaximumHalfAngle);
        float maximumSpread = Mathf.Clamp(weaponData.maxSpread, baseSpread, ProjectileSpread.MaximumHalfAngle);
        CurrentSpread = Mathf.MoveTowards(Mathf.Clamp(CurrentSpread, baseSpread, maximumSpread), baseSpread, Mathf.Max(0f,weaponData.recoveryRate) * recoveryTime);
    }

    private void HandleSpentCaseEjected()
    {
        if(!isActiveAndEnabled || casingEjectPoint == null || _pool == null || !_pool.IsReady) return;
        Casing casing = _pool.GetCasing(casingPoolKey);
        if(casing == null) return;

        casing.transform.SetPositionAndRotation(casingEjectPoint.position,casingEjectPoint.rotation);

        casing.Eject(casingEjectPoint.forward);    
    }

    private void OnDestroy()
    {
        if(_ammoSupply != null) _ammoSupply.SpentCaseEjected -= HandleSpentCaseEjected;        
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportConfigurationError(string message)
    {
        Debug.LogError(message, this);
    }
}
