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

    private float _lastFireTime = float.NegativeInfinity;

    public WeaponData Data => weaponData;
    public float CurrentSpread { get; private set; }

    // 발사 위치, 이번 발사에 사용한 탄퍼짐
    public event Action<Vector3, float> Fired;

    private void Awake()
    {
        SynchronizeWeaponData();
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
        if (!isActiveAndEnabled ||Time.deltaTime <= 0f || weaponData == null || muzzle == null || _pool == null || !_pool.IsReady || GameManager.Instance == null)
            return false;
        
        SynchronizeWeaponData();

        Bullet bullet = _pool.GetBullet(BulletPoolKey);

        if (bullet == null)
            return false;

        // 이번 단계는 기존 Bullet 프리팹의 탄약 데이터를 사용합니다.
        BulletData ammo = bullet.bulletData;

        if (ammo == null || ammo.speed <= 0f)
        {
            bullet.ReturnToPool();
            return false;
        }

        float baseSpread = Mathf.Max(0f, weaponData.baseSpread);
        float maximumSpread = Mathf.Max(baseSpread, weaponData.maxSpread);

        float shotSpread = Mathf.Clamp(CurrentSpread, baseSpread, maximumSpread);

        float randomYaw = UnityEngine.Random.Range(-shotSpread, shotSpread);

        Vector3 shotPosition = muzzle.position;

        Quaternion shotRotation = muzzle.rotation * Quaternion.Euler(0f, randomYaw, 0f);

        bullet.transform.SetPositionAndRotation(shotPosition, shotRotation);

        bullet.speed = ammo.speed;
        bullet.Init();

        _lastFireTime = Time.time;

        CurrentSpread = Mathf.Clamp(shotSpread + Mathf.Max(0f, weaponData.bloomPerShot),baseSpread,maximumSpread);

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
}