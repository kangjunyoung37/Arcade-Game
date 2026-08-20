using UnityEngine;

public struct DamageInfo
{
    public float damage;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public string hitTag;
    public float armorPenetration;
    public string bodyPart;
}
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}
