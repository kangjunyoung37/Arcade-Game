using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{

    private const string BulletPoolKey = "Bullet";
    private const string CasingPoolKey = "Casing";
    private const float SpreadRecoveryDelayMultiplier = 1.5f;

    [Header("CrossHair")]
    public Crosshair crosshair;
    public LayerMask groundLayer;
    
    [Header("Gravity")] 
    public float gravity = -15f;
    private float _velocityY;
    
    [Header("Character Stat")]
    public float normalspeed = 8f;
    public float runspeed = 12f;
    private float _currentSpeed = 8f;
    public float aimingSpeed = 4f;
    
    [Header("Shoot Settings")]
    private float _lastFireTime = float.NegativeInfinity;

    [Header("Shoot Test")]
    public Transform bulletSpawnPoint;
    public WeaponData currentWeaponData;
    
    //조준시 카메라 설정
    [Header("Camera Aim")] 
    public Transform cameraTarget;
    public float aimCameraDistance = 4.0f;
    public float aimCameraSpeed = 5f;

    [Header("Dodge Settings")]
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.2f;
    public float dodgeCooldown = 1f;
    
    [Header("AI Noise")]
    [SerializeField, Min(0f)]
    private float gunshotNoiseRadius = 30f;

    [SerializeField, Min(0f)]
    private float walkNoiseRadius = 6f;

    [SerializeField, Min(0f)]
    private float runNoiseRadius = 12f;

    [SerializeField, Min(0.1f)]
    private float footstepDistance = 1.5f;

    private Vector3 _lastFootstepPosition;
    private float _accumulatedFootstepDistance;
    //Component
    private HealthSystem _health;
    private Camera mainCamera;
    private CharacterController _characterControllercc;
    private CinemachineImpulseSource _impulseSource;
    private ObjectPoolManager _weaponPool;
    
    //Variable
    private bool _isAiming = false;
    private bool _sceneReferencesBound;
    private bool _isRunning = false;
    private bool _isDodging = false;
    private Vector3 _moveDirection;
    private float _rotateSpeed = 15f;
    private float _lastDodgeTime;

    public float CurrentSpread { get; private set; }
    public bool CanUseWeapon => isActiveAndEnabled &&_sceneReferencesBound && !_isRunning && !_isDodging && currentWeaponData != null;

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _characterControllercc = GetComponent<CharacterController>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        if (_health is not null)
        {
            _health.onDied += HandleDeath;
        }
    }

    private void Start()
    {
        if (!_sceneReferencesBound)
        {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("플레이어의 씬 참조가 초기화되지 않았습니다. " + "PlayerSpawnManager를 통해 생성해 주세요.", this);
    #endif
            enabled = false;
            return;
        }
        _weaponPool = ObjectPoolManager.instance;

        if (currentWeaponData != null)
            CurrentSpread = currentWeaponData.baseSpread;

        _lastFootstepPosition = transform.position;
    }
    
    private void Update()
    {

        Aiming();
        Running();
        if (crosshair is not null)
        {
            crosshair.SetAiming(_isAiming);
        }

        if (!_isDodging)
        {
            CharacterMovement();
            HandleAimAndCamera();
        }
        HandleSpreadRecovery();
        Dodge();
        RotateItem();
        UpdateFootstepNoise();
    }


    public bool InitializeSceneReferences( Camera gameplayCamera,Crosshair sceneCrosshair, Transform sceneAimTarget)
    {
        if (gameplayCamera == null ||
            sceneCrosshair == null ||
            sceneAimTarget == null)
        {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("플레이어의 카메라, 크로스헤어, 조준 카메라 타깃 연결이 필요합니다.", this);
    #endif
            return false;
        }

        mainCamera = gameplayCamera;
        crosshair = sceneCrosshair;
        cameraTarget = sceneAimTarget;

        cameraTarget.position = transform.position;

        _lastFootstepPosition = transform.position;
        _accumulatedFootstepDistance = 0f;

        _sceneReferencesBound = true;
        return true;
    }
    private void OnDestroy()
    {
        if (_health != null)
            _health.onDied -= HandleDeath;

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterCharacter(this);
    }
    
    private void CharacterMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        _moveDirection = (camForward * v) + (camRight * h).normalized;
        if (_characterControllercc.isGrounded && _velocityY < 0)
        {
            _velocityY = -2f;
        }
        _velocityY += gravity * Time.deltaTime;
        Vector3 finalMove = (_moveDirection * _currentSpeed) + (Vector3.up * _velocityY);
        _characterControllercc.Move(finalMove* Time.deltaTime);
        
    }

    private void HandleAimAndCamera()
    {
        Vector3 targetLookDirection = Vector3.zero;
        Vector3 targetCamPos = transform.position; 
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 hitPoint = hit.point;
            hitPoint.y = transform.position.y;
           
            targetLookDirection = hitPoint - transform.position;
            
            Vector3 dir = hitPoint - transform.position;
            Vector3 clampedDir = Vector3.ClampMagnitude(dir, aimCameraDistance); 
            targetCamPos = transform.position + clampedDir;
        }
        
        if(_isRunning)
            targetLookDirection = _moveDirection; 
        
        targetLookDirection.y = 0f; 
        if (targetLookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);
        }
        
        if (cameraTarget)
        {
            cameraTarget.position = Vector3.Lerp(cameraTarget.position, targetCamPos, Time.deltaTime * aimCameraSpeed);
        }
    }

    #region Fire Code

    private void HandleSpreadRecovery()
    {
        if (currentWeaponData == null)
            return;

        float recoveryDelay = currentWeaponData.GetShotInterval(currentWeaponData.fireMode) * SpreadRecoveryDelayMultiplier;

        if (Time.time < _lastFireTime + recoveryDelay)
            return;

        CurrentSpread = Mathf.MoveTowards(CurrentSpread, Mathf.Max(0f, currentWeaponData.baseSpread), Mathf.Max(0f, currentWeaponData.recoveryRate) * Time.deltaTime);
    }

    public bool TryFireWeapon()
    {
        if (!CanUseWeapon || bulletSpawnPoint == null || _weaponPool == null || !_weaponPool.IsReady || GameManager.Instance == null)
            return false;
        
        Bullet bullet = _weaponPool.GetBullet(BulletPoolKey);

        if (bullet == null)
            return false;

        // 이번 단계에서는 기존 Bullet 프리팹의 탄약 데이터를 사용
        if (bullet.bulletData == null || bullet.bulletData.speed <= 0f)
        {
            bullet.ReturnToPool();
            return false;
        }

        float baseSpread = Mathf.Max(0f, currentWeaponData.baseSpread);

        float maximumSpread = Mathf.Max( baseSpread, currentWeaponData.maxSpread);

        float shotSpread = Mathf.Clamp( CurrentSpread, baseSpread, maximumSpread);

        float randomYaw = Random.Range(-shotSpread, shotSpread);

        Quaternion spreadRotation = Quaternion.Euler(0f, randomYaw, 0f);

        bullet.transform.SetPositionAndRotation( bulletSpawnPoint.position, bulletSpawnPoint.rotation * spreadRotation);

        bullet.speed = bullet.bulletData.speed;
        bullet.Init();

        // 단발·연사·점사 모두 성공한 발사 시각을 기록합니다.
        _lastFireTime = Time.time;

        CurrentSpread = Mathf.Clamp( shotSpread + Mathf.Max(0f, currentWeaponData.bloomPerShot), baseSpread, maximumSpread);

        PlayerNoise.Emit( this, bulletSpawnPoint.position, gunshotNoiseRadius, PlayerNoiseType.Gunshot);

        if (crosshair != null)
            crosshair.AddSpread(shotSpread);

        if (_impulseSource != null)
            _impulseSource.GenerateImpulse();

        EjectCasing();

        return true;
    }

    private void EjectCasing()
    {
        if (_weaponPool == null || bulletSpawnPoint == null)
            return;

        Casing casing = _weaponPool.GetCasing(CasingPoolKey);

        // 탄피 연출이 없어도 이미 발사한 총알은 정상적으로 유지합니다.
        if (casing == null)
            return;

        casing.transform.SetPositionAndRotation(
            bulletSpawnPoint.position,
            bulletSpawnPoint.rotation);

        Vector3 ejectDirection =
            (bulletSpawnPoint.right + bulletSpawnPoint.up).normalized;

        casing.Eject(ejectDirection);
    }

    #endregion
   
    
    //Dodge
    #region Dodge

    private void Dodge()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > _lastDodgeTime + dodgeCooldown && !_isDodging)
        {
            var dashDirection = (_moveDirection.sqrMagnitude > 0) ?  _moveDirection : transform.forward;
            StartCoroutine(DodgeRoutine(dashDirection));
        }
        
    }
    private IEnumerator DodgeRoutine(Vector3 directrion)
    {
        _isDodging = true;
        _health.isInvincible = true;
        float startTime = Time.time;
        while (Time.time < startTime + dodgeDuration)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directrion), _rotateSpeed * Time.deltaTime);
            _characterControllercc.Move(directrion * (dodgeSpeed * Time.deltaTime));
            yield return null;
        }
        _isDodging = false;
        _health.isInvincible = true;
        _lastDodgeTime = Time.time;
    }

    #endregion

    private void Running()
    {
        if (_isAiming) return;
        
        if (Input.GetButtonDown("Run"))
        {
            _currentSpeed = runspeed;
            _isRunning = true;
        }

        if (Input.GetButtonUp("Run"))
        {
            _currentSpeed = normalspeed;
            _isRunning = false;
        }
    }

    private void Aiming()
    {
        _isAiming = Input.GetMouseButton(1);
        if (_isAiming)
        {
            _isRunning = false;
            _currentSpeed = aimingSpeed;
        }
        else
        {
            _currentSpeed = normalspeed;
        }
    }

    private void RotateItem()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (ItemDrag.CurrrentlyDraggedItem)
            {
                ItemDrag.CurrrentlyDraggedItem.RotateItem();
            }
        }
    }

private void UpdateFootstepNoise()
{
    Vector3 currentPosition = transform.position;
    Vector3 displacement = currentPosition - _lastFootstepPosition;
    _lastFootstepPosition = currentPosition;

    if (!_characterControllercc.isGrounded || _isDodging)
    {
        _accumulatedFootstepDistance = 0f;
        return;
    }

    displacement.y = 0f;

    _accumulatedFootstepDistance += displacement.magnitude;

    if (_accumulatedFootstepDistance < footstepDistance)
    {
        return;
    }

    _accumulatedFootstepDistance %= footstepDistance;

    float radius = _isRunning
        ? runNoiseRadius
        : walkNoiseRadius;

    PlayerNoise.Emit(this, currentPosition, radius, PlayerNoiseType.Footstep);
}
    private void HandleDeath()
    {
        Debug.Log("죽음");
    }
}
