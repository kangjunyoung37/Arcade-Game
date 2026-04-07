using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MiningNPC : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 3f;
    public Transform pointA;
    public Transform pointB;
    public MiningTool miningTool;
    
    private Transform currentTarget;
    private Tween moveTween; // 현재 진행 중인 이동 애니메이션을 저장할 변수
    private bool isMining = false;
    private Furnace _furnace;
    public Mineral targetMineral;
    
    void Start()
    {
        _furnace = GameManager.Instance.GetFurnace();
        // 처음엔 도착점(B)을 향해 출발합니다.
        currentTarget = pointB;
        transform.LookAt(currentTarget);
        
        // 이동 시작
        StartPatrol();
    }
    void StartPatrol()
    {
        // 남은 거리와 시간을 계산하여 속도를 일정하게 유지합니다.
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        float duration = distance / moveSpeed;

        // DOMove 실행 후 변수에 저장
        moveTween = transform.DOMove(currentTarget.position, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 목적지 도착 시 타겟을 반대로 변경 (A <-> B)
                currentTarget = (currentTarget == pointA) ? pointB : pointA;
                
                // 자연스럽게 뒤로 돌기
                transform.DOLookAt(currentTarget.position, 0.4f, AxisConstraint.Y)
                    .OnComplete(() => 
                    {
                        // 다 돌면 다시 출발
                        StartPatrol(); 
                    });
            });
    }
    
    public void MiningWithTool()
    {
        if (targetMineral != null && targetMineral.isAvailable)
        {
            GameManager.Instance.GetCharacter().RemoveMineral(targetMineral);
            targetMineral.Mine();
            targetMineral = null;
            var rk = ObjectPoolManager.instance.GetGo("Rock");
            Rock rock = rk.GetComponent<Rock>();
            rock.transform.position = transform.position;
            _furnace.GetRock(rock);
        }
    }

    public void PauseMove()
    {
        isMining = true;

        // 💡 핵심: 걷고 있던 DOMove 애니메이션을 일시 정지합니다.
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Pause();
        }
        
    }
    
    public void PlayMove()
    {
        // 💡 핵심: 멈춰있던 DOMove 애니메이션을 다시 재생하여 마저 걸어갑니다.
        isMining = false;
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Play();
        }
        
    }
    
}
