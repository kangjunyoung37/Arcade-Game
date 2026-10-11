using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyWeapon : MonoBehaviour
{
    private const string DefaultBulletPoolKey = "EnemyBullet";
    private const float DirectionSqrEpsilon = 0.0001f;
    [Header("References")]
    [SerializeField] private Transform muzzle;

    [Header("Weapon")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private BulletData reserveAmmo;


    [Header("Projectile")]
    [SerializeField] private string bulletPoolKey = DefaultBulletPoolKey;
    private ObjectPoolManager _pool;
    private InfiniteReserveAmmo _ammoSupply;
    private EnemyController _owner;
    public WeaponData Data => weaponData;

    public bool IsReloading => _ammoSupply != null && _ammoSupply.IsReloading;
    public int LoadedRounds => _ammoSupply != null ? _ammoSupply.TotalLoadedRounds : 0;

    public bool CanFire => isActiveAndEnabled && _ammoSupply != null && _ammoSupply.CanFire;


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
        if(weaponData.actionType != WeaponActionType.SelfLoading || weaponData.reloadType != WeaponReloadType.MagazineSwap || reserveAmmo. projectilesPerRound != 1)
        {
            ReportConfigurationError("이번 구현은 자동 작동식·탄창 교체형·단일 발사체만 지원합니다. ");
            enabled = false;
            return;
        }
        _ammoSupply = new InfiniteReserveAmmo(weaponData.caliber, weaponData.magazineSize, reserveAmmo, weaponData.reloadTime);

        _owner = GetComponent<EnemyController>();
    }

    private void Start()
    {
        _pool = ObjectPoolManager.instance;
    }

    private void Update()
    {
        _ammoSupply?.Tick(Time.deltaTime);
    }

    public bool TryFire(Vector3 aimPosition)
    {
        if (!CanFire || muzzle == null || _pool == null || !_pool.IsReady || GameManager.Instance == null)
        {
            return false;
        }

        Vector3 spawnPosition = muzzle.position;
        Vector3 direction = aimPosition - spawnPosition;

        if (direction.sqrMagnitude <= DirectionSqrEpsilon)
        {
            return false;
        }

        Bullet bullet = _pool.GetBullet(bulletPoolKey);

        if (bullet == null)
        {
            return false;
        }

        if (!_ammoSupply.TryConsumeRound(out BulletData ammo))
        {
            bullet.Pool.Release(bullet.gameObject);
            return false;
        }

        bullet.transform.SetPositionAndRotation(
            spawnPosition,
            Quaternion.LookRotation(direction));

        bullet.bulletData = ammo;
        bullet.speed = ammo.speed;
        bullet.Init();

        return true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ReportConfigurationError(string message)
    {
        Debug.LogError(message, this);
    }
}
