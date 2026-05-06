using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : PoolAble , IUpdateable
{
    [Header("Bullet Settings")]
    public float speed = 30.0f;
    public LayerMask hitMask;
    public BulletData bulletData;
    
    private TrailRenderer _trail;
    private MeshRenderer _meshRenderer;
    private Vector3 _previousPosition;
    private bool _isDead = false;
    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();

    }

    private void OnEnable()
    {
        _previousPosition = transform.position;
        GameManager.Instance.AddBullet(this);
    }
    private void OnDisable()
    {
        _trail.emitting = false;
        GameManager.Instance.RemoveBullet(this);
    }
    
    public void OnTick(float deltaTime)
    {
        if (_isDead) return;
        HitCheck(deltaTime);
        if(!_isDead)
            BulletMove(deltaTime);
    }

    private void BulletMove(float deltaTime)
    {
        transform.position += transform.forward * (speed * deltaTime);
        _previousPosition = transform.position;
    }

    public void Init()
    { 
        _trail.emitting = true;
        _trail.Clear();
        _isDead = false;
        _meshRenderer.enabled = true;
    }

    private void HitCheck(float deltaTime)
    {
        float distanceThisFrame = speed * deltaTime;
        if (Physics.Raycast(_previousPosition, transform.forward, out RaycastHit hit, distanceThisFrame, hitMask))
        {
            if (hit.collider.TryGetComponent(out HealthSystem healthSystem))
            {
                if (healthSystem.isInvincible)
                    return;
                DamageInfo damageInfo = new DamageInfo
                {
                    damage = bulletData.damage
                };
                healthSystem.TakeDamage(damageInfo);
            }
            Spawn_VFX(hit);
            _isDead = true;
            _trail.emitting = false;
            _meshRenderer.enabled = false;
            transform.position = hit.point;
            
            StartCoroutine(DelayDisable(_trail.time));
        }
    }

    private void Spawn_VFX(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent(out SurfaceProp hitSurface))
        {
            var vfxName = hitSurface.surfaceType switch
            {
                SurfaceType.Metal => "MetalVFX",
                SurfaceType.Flesh => "BloodVFX",
                SurfaceType.Wood => "WoodVFX",
                SurfaceType.Dirt => "DirtVFX",
                _ => "DefaultVFX"
            };
            var hitVFX = ObjectPoolManager.instance.GetGo(vfxName);
            if (!hitVFX) return;
            hitVFX.transform.position = hit.point;
            hitVFX.transform.rotation = Quaternion.LookRotation(hit.normal);
        }
    }
    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseObject();
    }
}
 