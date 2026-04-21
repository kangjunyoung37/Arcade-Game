using System;
using System.Collections.Generic;
using UnityEngine;



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
    
    [Header("캐릭터 스탯")]
    [SerializeField]
    public float normalspeed = 8f;
    public float aimingSpeed = 4f;


    private MainCamera _mainCamera;
    private Camera mainCamera;

    [Header("Shoot Test")]
    public Transform bulletSpawnPoint;
    
    //조준시 카메라 설정
    [Header("Camera Aim")] 
    public Transform cameraTarget;
    public float aimCameraDistance = 4.0f;
    public float aimCameraSpeed = 5f;
    
    private void Start()
    {
        _characterControllercc = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        _mainCamera = mainCamera.GetComponent<MainCamera>();
    }
    
    void Update()
    {
        CharacterMovement();
        _isAiming = Input.GetMouseButton(1);
        if (crosshair is not null)
        {
            crosshair.SetAiming(_isAiming);
        }
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
        HandleAimAndCamera();
        
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

    private void Fire()
    {
        if (crosshair is not null)
        {
            crosshair.AddSpread();
            GameObject bullet = ObjectPoolManager.instance.GetGo("Bullet");
            bullet.transform.position = bulletSpawnPoint.position;
            bullet.transform.rotation = bulletSpawnPoint.rotation;
            if (bullet.TryGetComponent(out Bullet t))
            {
                t.Init();
            }
        }
    }
    
}