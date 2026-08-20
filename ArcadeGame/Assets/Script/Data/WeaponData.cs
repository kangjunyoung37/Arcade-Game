using UnityEngine;

public enum FireMode
{
    Single,
    Auto,
    Burst
}
[CreateAssetMenu(fileName = "NewWeapon_Data", menuName = "Shooting/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    [Header("Fire Settings")]
    public FireMode fireMode;
    public float fireRate = 0.1f;
    public float singleFireDelay = 0.2f;
    public float burstPostDelay = 0.5f;
    public int burstCount = 3;

    [Header("Dynamic Spread Settings")] 
    public float baseSpread = 2f;
    public float maxSpread = 15f;
    public float bloomPerShot = 3f; // 쏠 때마다 벌어지는 각도
    public float recoveryRate = 10f; // 회복
    
    [Header("Weapon Settings")]
    public float reloadTime;
    public int magazineSize;
    
}
