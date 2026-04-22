using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{
    [Header("CrossHair")]
    public Crosshair crosshair;
    
    public LayerMask groundLayer;
    private CharacterController _characterControllercc;

    [Header("Gravity")] 
    public float gravity = -15f;
    private float _velocityY;
   
    private bool _isAiming = false;
    private Vector3 _moveDirection;
    private float _rotateSpeed = 15f;
    
    [Header("Character Stat")]
    [SerializeField]
    public float normalspeed = 8f;
    public float aimingSpeed = 4f;
    

    [Header("Shoot Settings")]
    private float _lastFireTime;
    private bool _isCooldown = false;
    public float CurrentSpread { get; private set; }
    
    [Header("Shoot Test")]
    public Transform bulletSpawnPoint;
    public WeaponData currentWeaponData;
    
    //조준시 카메라 설정
    [Header("Camera Aim")] 
    public Transform cameraTarget;
    public float aimCameraDistance = 4.0f;
    public float aimCameraSpeed = 5f;

    private HealthSystem _health;
    private MainCamera _mainCamera;
    private Camera mainCamera;
    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _characterControllercc = GetComponent<CharacterController>();
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
        CharacterMovement();
        _isAiming = Input.GetMouseButton(1);
        if (crosshair is not null)
        {
            crosshair.SetAiming(_isAiming);
        }
        TryFire();
        HandleAimAndCamera();
        HandleSpreadRecovery();
    }

    private void OnDestroy()
    {
        if (_health is not null)
        {
            _health.onDied -= HandleDeath;
        }
    }

    void CharacterMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        float characterSpeed = _isAiming ? aimingSpeed : normalspeed;
        _moveDirection = (camForward * v) + (camRight * h).normalized;
        if (_characterControllercc.isGrounded && _velocityY < 0)
        {
            _velocityY = -2f;
        }
        _velocityY += gravity * Time.deltaTime;
        Vector3 finalMove = (_moveDirection * characterSpeed) + (Vector3.up * _velocityY);
        _characterControllercc.Move(finalMove* Time.deltaTime);
        
    }
    void HandleAimAndCamera()
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
        
        if(!_isAiming)
            targetLookDirection = _moveDirection; 
        
        targetLookDirection.y = 0f; 
        if (targetLookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);
        }
        
        if (cameraTarget != null)
        {
            cameraTarget.position = Vector3.Lerp(cameraTarget.position, targetCamPos, Time.deltaTime * aimCameraSpeed);
        }
    }

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
        if (_isCooldown) return;
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
        GameObject bullet = ObjectPoolManager.instance.GetGo("Bullet");
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
    private void HandleDeath()
    {
        Debug.Log("죽음");
    }
}