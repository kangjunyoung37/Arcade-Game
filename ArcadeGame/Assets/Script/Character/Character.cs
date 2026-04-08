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
    [SerializeField]
    public MiningTool miningTool;

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
    [Header("채광 기술")]
    [SerializeField] public List<MiningTool> miningTools;
    [Header("탐지된 광물 리스트")]
    [SerializeField]
    private List<Mineral> minerals = new List<Mineral>();
    private MainCamera _mainCamera;
    private Camera mainCamera;
    private int _curMineMode = 0;
    
    //조준시 카메라 설정
    [Header("Camera Aim")] 
    public Transform cameraTarget;
    public float aimCameraDistance = 4.0f;
    public float aimCameraSpeed = 5f;
    
    [Header("화살표")]
    public NavigationArrow navArrow;
    
    public Inventory inventory; 
    private void Start()
    {
        _characterControllercc = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        mainCamera = Camera.main;
        _mainCamera = mainCamera.GetComponent<MainCamera>();
        ChangeMineTool(0);
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
        }
    }
    public void AddMineral(Mineral mineral)
    {
        minerals.Add(mineral);
    }

    public void RemoveMineral(Mineral mineral)
    {
        if(minerals.Contains(mineral))
            minerals.Remove(mineral);
    }

    //탐지된 광물 체크
    public bool MineralCheck()
    {
        if (minerals.Count == 0 || !minerals[0].isAvailable)
            return false;
        return true;
    }
    
    //광물 캐기
    public void MiningWithTool()
    {
        if (!MineralCheck())
            return;
        minerals[0].Mine();
        RemoveMineral(minerals[0]);
        inventory.AddRock();
        
    }

    public void ChangeMineTool(int num)
    {
        _curMineMode = num;
        miningTool =  miningTools[num];
        inventory.maxMineral = miningTool.maxMineral;
    }
    public void SwitchMineMode(bool switchBool)
    {
        if (_curMineMode == 2)
        {
            float targetY = switchBool ? 2.15f : 0.87f;
            
            Vector3 moveOffset = new Vector3(0, targetY - transform.position.y, 0);

            _characterControllercc.Move(moveOffset);
        }
        miningTool.gameObject.SetActive(switchBool);
        
    }
    
}