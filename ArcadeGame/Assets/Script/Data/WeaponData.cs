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
    public AmmoCaliberData ammoCaliber;

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
    
    public int ShotsPerCycle => fireMode == FireMode.Burst ? Mathf.Max(burstCount, 1) : 1;
    public float ShotInterval => Mathf.Max(MinimumShotInterval, fireRate);
    public float CycleCooldown
    {
        get
        {
            float interval = fireMode switch
        {
            FireMode.Single => singleFireDelay,
            FireMode.Auto   => fireRate,
            FireMode.Burst  => burstPostDelay,
            _               => singleFireDelay
        };
        
            return Mathf.Max(MinimumShotInterval, interval);
        }
    }

}
