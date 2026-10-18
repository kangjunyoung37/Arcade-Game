using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet_Data", menuName = "Shooting/BulletData")]
public class BulletData : ScriptableObject
{
   private const float MinimumPositiveValue = 0.01f;
   [Header("Identity")]
   public string bulletName;
   public AmmoCaliberData caliber;

   [Header("Damage"), Tooltip("기본 피해량"), Min(0f)]
   public float damage = 20f;

   [Tooltip("방어구 관통"), Min(0f)]
   public int penetrationLevel;

   [Header("Projectiles per Round"), Tooltip("탄환 1발당 발사되는 투사체 수"), Min(1)]
   public int projectilesPerRound = 1;

   [Tooltip("탄 퍼짐의 각도"), Range(0f, 90f)]
   public float pelletSpreadHalfAngle;

   [Header("Projectile Movement")]
   [Min(MinimumPositiveValue)] public float speed = 60f;

   [Min(MinimumPositiveValue)] public float lifetime = 5f;

#if UNITY_EDITOR
   private void OnValidate()
   {
        damage = Mathf.Max(0f, damage);
        penetrationLevel = Mathf.Max(0, penetrationLevel);
        projectilesPerRound = Mathf.Max(1, projectilesPerRound);

        pelletSpreadHalfAngle = Mathf.Clamp(pelletSpreadHalfAngle, 0f, 90f);

        speed = Mathf.Max(MinimumPositiveValue, speed);
        lifetime = Mathf.Max(MinimumPositiveValue, lifetime);
   }
#endif   
}
