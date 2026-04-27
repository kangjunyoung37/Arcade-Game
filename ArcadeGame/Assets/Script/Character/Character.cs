using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{
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
    private float _lastFireTime;
    private bool _isCooldown = false;

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
    
    //Component
    private HealthSystem _health;
    private MainCamera _mainCamera;
    private Camera mainCamera;
    private CharacterController _characterControllercc;
    private CinemachineImpulseSource _impulseSource;
    
    //Variable
    private bool _isAiming = false;
    private Vector3 _moveDirection;
    private float _rotateSpeed = 15f;
    private bool _isDodging = false;
    private float _lastDodgeTime;
    private bool _isRunning = false;
    public float CurrentSpread { get; private set; }

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
        mainCamera = Camera.main;
        _mainCamera = mainCamera.GetComponent<MainCamera>();
        if (currentWeaponData is not null)
        {
            CurrentSpread = currentWeaponData.baseSpread;
        }
    }
    
    private void Update()
    {

        Aiming();
        Running();
        if (crosshair is not null)
        {
            crosshair.SetAiming(_isAiming);
        }
        
        TryFire();
        if (!_isDodging)
        {
            CharacterMovement();
            HandleAimAndCamera();
        }
        HandleSpreadRecovery();
        Dodge();
       
   
    }

    private void OnDestroy()
    {
        if (_health is not null)
        {
            _health.onDied -= HandleDeath;
        }
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
        if (Time.time > _lastFireTime + currentWeaponData.fireRate * 1.5f)
        {
            CurrentSpread = Mathf.MoveTowards(CurrentSpread, currentWeaponData.baseSpread,
                currentWeaponData.recoveryRate * Time.deltaTime);
        }
    }
    private void TryFire()
    {
        if (_isCooldown || _isRunning || _isDodging) return;
        switch (currentWeaponData.fireMode)
        {
            case FireMode.Single:
                if (Input.GetMouseButtonDown(0))
                {
                    Fire();
                    StartCoroutine(FireCooldown(currentWeaponData.singleFireDelay));
                }
                break;

            case FireMode.Auto:
                if (Input.GetMouseButton(0) && Time.time >= _lastFireTime + currentWeaponData.fireRate)
                {
                    Fire();
                    _lastFireTime = Time.time;
                }
                break;

            case FireMode.Burst:
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(BurstFireRoutine());
                }
                break;
        }
    }
    
    //Bullet Fire
    private void Fire()
    {
        crosshair.AddSpread(CurrentSpread);
        _impulseSource.GenerateImpulse();
        GameObject casing = ObjectPoolManager.instance.GetGo("Casing");
        GameObject bullet = ObjectPoolManager.instance.GetGo("Bullet");
        //Test Casing
        casing.transform.position = bulletSpawnPoint.position;
        casing.transform.rotation = bulletSpawnPoint.rotation;
        
        var perfectDirection = (bulletSpawnPoint.right +  bulletSpawnPoint.up).normalized;
        if (casing.TryGetComponent(out Casing casingComponent))
            casingComponent.Eject(perfectDirection);
        
        float randowYaw = Random.Range(-CurrentSpread, CurrentSpread);
        Quaternion spreadRotation = Quaternion.Euler(0, randowYaw, 0);
        
        bullet.transform.position = bulletSpawnPoint.position;
        bullet.transform.rotation = bulletSpawnPoint.rotation * spreadRotation;
        CurrentSpread += currentWeaponData.bloomPerShot;
        CurrentSpread = Mathf.Clamp(CurrentSpread, currentWeaponData.baseSpread, currentWeaponData.maxSpread);
        if (bullet.TryGetComponent(out Bullet b))
        {
            b.Init();
        }
        
    }
    
    private IEnumerator FireCooldown(float delay)
    {
        _isCooldown = true;
        yield return new WaitForSeconds(delay);
        _isCooldown = false;
    }

    private IEnumerator BurstFireRoutine()
    {
        _isCooldown = true; 
        for (int i = 0; i < currentWeaponData.burstCount; i++)
        {
            Fire();
            yield return new WaitForSeconds(currentWeaponData.fireRate);
        }
        
        yield return new WaitForSeconds(currentWeaponData.burstPostDelay);
        _isCooldown = false;
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
    }
    private void HandleDeath()
    {
        Debug.Log("죽음");
    }
    private void OnDrawGizmos()
    {
        if (bulletSpawnPoint != null)
        {
            // 배출구 위치에서 오른쪽(빨간색) 방향으로 1미터짜리 빨간 선을 그어줍니다!
            Gizmos.color = Color.red;
            Gizmos.DrawRay(bulletSpawnPoint.position, bulletSpawnPoint.right * 1f);

            // 배출구 위치에서 위쪽(초록색) 방향으로 1미터짜리 초록 선을 그어줍니다!
            Gizmos.color = Color.green;
            Gizmos.DrawRay(bulletSpawnPoint.position, bulletSpawnPoint.up * 1f);
        }
    }
}