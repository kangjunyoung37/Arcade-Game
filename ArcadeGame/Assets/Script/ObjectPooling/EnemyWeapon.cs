using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyWeapon : MonoBehaviour
{
    private const string DefaultBulletPoolKey = "EnemyBullet";
    private const float DirectionSqrEpsilon = 0.0001f;
    [Header("References")]
    [SerializeField] private Transform muzzle;

    [Header("Projectile")]
    [SerializeField] private string bulletPoolKey = DefaultBulletPoolKey;

    private EnemyController _owner;
    private ObjectPoolManager _pool;

    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
    }
    private void OnEnable()
    {
        if(_owner != null)
        {
            _owner.AttackRequested += HandleAttackRequested;
        }
    }
    private void Start()
    {
        _pool = ObjectPoolManager.instance;
    }

    private void OnDisable()
    {
                  if(_owner != null)
        {
            _owner.AttackRequested -= HandleAttackRequested;
        }     
    }
    private void HandleAttackRequested(Vector3 aimPosition)
    {
        if(muzzle == null || _pool == null || !_pool.IsReady || GameManager.Instance == null)
            return;
        Vector3 spawnPosition = muzzle.position;
        Vector3 direction = aimPosition - spawnPosition;
        if(direction.sqrMagnitude <= DirectionSqrEpsilon)
            return;
        Bullet bullet = _pool.GetBullet(bulletPoolKey);
        if(bullet == null)
            return;
        bullet.transform.SetPositionAndRotation(spawnPosition,Quaternion.LookRotation(direction));

        bullet.Init();
        
    }
}
