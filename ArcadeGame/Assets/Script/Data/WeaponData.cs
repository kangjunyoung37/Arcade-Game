using System;
using UnityEngine;

public enum FireMode
{
    Single = 0,
    Auto = 1,
    Burst = 2
}

public enum WeaponCategory
{
    Pistol = 0,
    Rifle = 1,
    Sniper = 2,
    Shotgun = 3,
    SMG = 4,
    LMG = 5
}

public enum WeaponActionType
{
    SelfLoading = 0,
    BoltAction = 1,
    PumpAction = 2
}

public enum WeaponReloadType
{
    MagazineSwap,
    InsertOneRound
}

[CreateAssetMenu(fileName = "NewWeapon_Data", menuName = "Shooting/Weapon Data")]
public class WeaponData : ScriptableObject
{
    
    public const float MinimumShotInterval = 0.02f;

    [Header("Identity")]
    public string weaponName;
    public WeaponCategory category;

    [Header("Ammunition Compatibility")]
    public AmmoCaliberData caliber;

    [Header("Weapon Action")]
    public WeaponActionType actionType = WeaponActionType.SelfLoading;

    [Header("Fire Mode"),Tooltip("기본 발사 모드")] 
    public FireMode fireMode = FireMode.Single;

    [SerializeField, Tooltip("기본 모드 외에 추가로 지원하는 발사 모드")]
    private FireMode[] additionalFireModes;

    [Header("Fire Timing - Seconds"), Tooltip("자동사격의 발사 간격"), Min(MinimumShotInterval)]
    public float fireRate = 0.1f;
    [Min(MinimumShotInterval)]
    public float singleFireDelay = 0.2f;
    [Min(MinimumShotInterval)]
    public float burstShotInterval = 0.1f;
    [Min(1)]
    public int burstCount = 3;

    [Min(MinimumShotInterval), Tooltip("점사의 마지막 발 이후 대기 시간")]
    public float burstPostDelay = 0.5f;

    [Tooltip("볼트 또는 펌프 조작 시간. SelfLoading에서는 사용하지 않음"), Min(0f)]
    public float actionCycleTime = 0.8f;
    
    [Header("Dynamic Spread - Degrees")] 
    [Min(0f)]
    public float baseSpread = 2f;
    [Min(0f)]
    public float maxSpread = 15f;
    [Min(0f)]
    public float bloomPerShot = 3f; // 쏠 때마다 벌어지는 각도
    [Min(0f), Tooltip("탄 퍼짐 회복 각도")]
    public float recoveryRate = 10f; // 회복
    
    [Header("Reload")]
    public WeaponReloadType reloadType = WeaponReloadType.MagazineSwap;

    [Tooltip("탄창 또는 내부 저장공간의 용량, 약실은 별도 관리")]
    [Min(1)]
    public int magazineSize = 30;

    [Tooltip("탄창 교체 시간"),Min(MinimumShotInterval)]
    public float reloadTime = 2f;

    [Tooltip("탄약을 한 발 삽입하는 데 걸리는 시간"),Min(MinimumShotInterval)]
    public float insertRoundTime = 0.6f;
    
    public int ShotsPerCycle => GetShotsPerCycle(fireMode);

    public float ShotInterval => GetShotInterval(fireMode);
    public float CycleCooldown => GetCycleCooldown(fireMode);

    public bool SupportsFireMode(FireMode mode)
    {
        if (mode == fireMode) return true;
        if(additionalFireModes == null) return false;
        for(int i = 0; i < additionalFireModes.Length; i++)
        {
            if (additionalFireModes[i] == mode) return true;
        }
        return false;
    }
    public bool IsAmmoCompatible(BulletData ammo)
    {
        return ammo != null && caliber != null && ammo.caliber == caliber;
    }
    public int GetShotsPerCycle(FireMode mode)
    {
        return mode == FireMode.Burst ? Mathf.Max(1, burstCount) : 1;
    }

    public float GetShotInterval(FireMode mode)
    {
        float interval = mode switch
        {
            FireMode.Single => singleFireDelay,
            FireMode.Auto => fireRate,
            FireMode.Burst => burstShotInterval,
            _ => singleFireDelay
        };
        return Mathf.Max(MinimumShotInterval, interval);
    }

    public float GetCycleCooldown(FireMode mode)
    {
        return mode == FireMode.Burst ? Mathf.Max(MinimumShotInterval, burstPostDelay) : GetShotInterval(mode);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        fireRate = Mathf.Max(MinimumShotInterval, fireRate);
        singleFireDelay = Mathf.Max(MinimumShotInterval, singleFireDelay);
        burstShotInterval = Mathf.Max(MinimumShotInterval, burstShotInterval);
        burstPostDelay = Mathf.Max(MinimumShotInterval, burstPostDelay);

        burstCount = Mathf.Max(1, burstCount);
        magazineSize = Mathf.Max(1, magazineSize);

        actionCycleTime = Mathf.Max(0f, actionCycleTime);
        reloadTime = Mathf.Max(MinimumShotInterval, reloadTime);
        insertRoundTime = Mathf.Max(MinimumShotInterval, insertRoundTime);

        baseSpread = Mathf.Max(0f, baseSpread);
        maxSpread = Mathf.Max(baseSpread, maxSpread);
        bloomPerShot = Mathf.Max(0f, bloomPerShot);
        recoveryRate = Mathf.Max(0f, recoveryRate);
    }
#endif
}
