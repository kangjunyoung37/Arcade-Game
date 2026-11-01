using System.Collections;
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
    [SerializeField, Min(0.01f)]
    private float fallbackLifetime = 5f;

    private float _remainingLifetime;
    private float _remainingReturnDelay;
    private GameManager _registeredManager;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();

    }

    private void OnEnable()
    {
        _isDead = true;
        _remainingReturnDelay = float.PositiveInfinity;
    }
    private void OnDisable()
    {
        if(_trail != null)
        {
            _trail.emitting = false;
        }
        if(_registeredManager != null)
        {
            _registeredManager.RemoveBullet(this);
            _registeredManager = null;
        }
    }
    
    public void OnTick(float deltaTime)
    {
        if(_isDead)
        {
            _remainingReturnDelay -= deltaTime;
            if(_remainingReturnDelay <= 0f)
            {
                ReleaseObject();
            }
            return;
        }
        
        _remainingLifetime -= deltaTime;
        if(_remainingLifetime <= 0f)
        {
            ReleaseObject();
            return;
        }
        HitCheck(deltaTime);
        if (!_isDead)
        {
            BulletMove(deltaTime);
        }
    }

    private void BulletMove(float deltaTime)
    {
        transform.position += transform.forward * (speed * deltaTime);
        _previousPosition = transform.position;
    }

    public void Init()
    { 
        GameManager manager = GameManager.Instance;
        if(manager == null || bulletData == null)
        {
            ReleaseObject();
            return;
        }
        _previousPosition = transform.position;

        _remainingLifetime = bulletData.lifetime > 0f ? bulletData.lifetime : fallbackLifetime;
        _remainingReturnDelay = 0f;
        _isDead = false;
        if(_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }
        if(_meshRenderer != null)
        {
            _meshRenderer.enabled = true;
        }
        _registeredManager = manager;
        _registeredManager.AddBullet(this);

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
            transform.position = hit.point;
            if(_trail != null)
                _trail.emitting = false;
            if(_meshRenderer != null)
                _meshRenderer.enabled = false;
            
            _remainingReturnDelay = _trail != null ? _trail.time : 0f;    
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
 