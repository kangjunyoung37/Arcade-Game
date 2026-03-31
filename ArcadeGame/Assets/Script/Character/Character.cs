using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;


[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour
{
    public FloatingJoystick joystick;
    
    
    private CharacterController _characterControllercc;
    [SerializeField]
    public MiningTool miningTool;
    
   
    
    [Header("캐릭터 스탯")]
    [SerializeField]
    public float speed = 4f;
    [Header("채광 기술")]
    [SerializeField] public List<MiningTool> miningTools;
    [Header("탐지된 광물 리스트")]
    [SerializeField]
    private List<Mineral> minerals = new List<Mineral>();
    private MainCamera _mainCamera;
    private Camera mainCamera;
    private int _curMineMode = 0;
    
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
    }

    void CharacterMovement()
    {
        // 쿼터뷰 기준 이동 (카메라 방향 기준)
        float h = joystick.Horizontal;
        float v = joystick.Vertical;
        
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        
        Vector3 moveDirection = (camForward * v) + (camRight * h);
        moveDirection.Normalize();
        
        _characterControllercc.Move(moveDirection * (speed * Time.deltaTime));
        if (moveDirection.magnitude >= 0.1f)
        {
            Quaternion rotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,rotation, 180);
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