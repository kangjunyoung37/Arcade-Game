using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet_Data", menuName = "Shooting/BulletData")]
public class BulletData : ScriptableObject
{
   public string bulletName;
   public float damage;
   public float speed;
   public float lifetime;
   
}
