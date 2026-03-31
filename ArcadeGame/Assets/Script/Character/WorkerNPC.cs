using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class WorkerNPC : MonoBehaviour
{
    [Header("이동 설정")] public float moveSpeed = 3f;
    public Transform counterPoint; // 카운터 위치
    public Transform furnacePoint; // 용광로 위치
    public Transform handcuffsStackPoint;
    
    [Header("상태 (디버그용)")] public int carriedHandcuffs = 0; // 현재 들고 있는 수갑 개수
    public int maxCarry = 10; // 한 번에 들 수 있는 최대 개수

    private FrontDesk _frontDesk;
    private HandCuffsStorge _handcuffsStorge;

    private Stack<Handcuffs> _carryHandcuffsStack = new Stack<Handcuffs>();
    
    private float _height = 0.2f;
    void Start()
    {
        
        _frontDesk = GameManager.Instance.GetFrontDesk();
        _handcuffsStorge = GameManager.Instance.GetHandcuffsStorge();
        // 게임이 시작되면 NPC의 영원한 노동(코루틴)이 시작됩니다.
        StartCoroutine(WorkerRoutine());    
    }

    private IEnumerator WorkerRoutine()
    {
        while (true) // 무한 반복
        {
            // 1. 카운터에서 대기하며 수갑이 부족한지 감시합니다.
            yield return StartCoroutine(WaitAtCounter());

            // 2. 수갑이 부족해지면 용광로로 걸어갑니다.
            yield return StartCoroutine(MoveTo(furnacePoint));

            // 3. 용광로에 도착해서 수갑을 줍습니다. (작업 시간 대기)
            yield return StartCoroutine(PickupHandcuffs());

            // 4. 수갑을 다 주웠으면 다시 카운터로 걸어갑니다.
            yield return StartCoroutine(MoveTo(counterPoint));

            // 5. 카운터에 수갑을 내려놓습니다.
            yield return DropOffHandcuffs();
        }
    }

    // --- 아래는 각각의 디테일한 행동(대본)들입니다 ---

    private IEnumerator WaitAtCounter()
    {
        // TODO: 실제 카운터 스크립트의 수갑 개수를 체크해야 합니다.
        // 예: while (Counter.Instance.currentHandcuffs >= 5) yield return null;
        while(_frontDesk.curHandcuffs >= 1 || _handcuffsStorge.handcuffsStack.Count == 0) yield return new WaitForSeconds(1f);
        Debug.Log("카운터 대기 중... (수갑이 부족해질 때까지 대기)");

        // 지금은 테스트를 위해 카운터에서 2초 대기 후 출발하도록 임시 작성했습니다.
        //yield return new WaitForSeconds(2f);
    }

    private IEnumerator MoveTo(Transform target)
    {
        // 거리 계산 및 방향 돌리기
        float distance = Vector3.Distance(transform.position, target.position);
        float duration = distance / moveSpeed;

        transform.DOLookAt(target.position, 0.2f, AxisConstraint.Y);

        // ⭐ 마법의 코드: 이동 애니메이션이 '완전히 끝날 때까지' 이 코루틴을 잠시 멈추고 기다립니다!
        yield return transform.DOMove(target.position, duration)
            .SetEase(Ease.Linear)
            .WaitForCompletion();
    }

    private IEnumerator PickupHandcuffs()
    {
        Debug.Log("용광로 도착! 수갑 줍는 중...");
        // 줍는 애니메이션 재생 (예: 1초 소요)
        while (true)
        {
            if(_handcuffsStorge.handcuffsStack.Count == 0)
                yield break;
            Handcuffs handcuffs = _handcuffsStorge.handcuffsStack.Pop();
            
            _carryHandcuffsStack.Push(handcuffs);
            handcuffs.MoveToTransform(handcuffsStackPoint,new Vector3(0,_height * carriedHandcuffs,0));
            carriedHandcuffs++;
            _handcuffsStorge._cnt--;
            yield return new WaitForSeconds(0.05f);
        }
        
        // (이때 시각적으로 등에 수갑 모델링을 켜주면 좋습니다)
    }

    IEnumerator DropOffHandcuffs()
    {
        //Debug.Log($"카운터 도착! 수갑 {_carryHandcuffsStack.Count}개 보충 완료!");
        
        _frontDesk.movingHandcuffs = true;
        while (carriedHandcuffs != 0)
        {
            Handcuffs handcuffs = _carryHandcuffsStack.Pop();
            handcuffs.MoveToTransform(_frontDesk.stackPoint, new Vector3(0,_height * _frontDesk.curHandcuffs,0));
            _frontDesk.handcuffsStack.Push(handcuffs);
            _frontDesk.curHandcuffs++;
            carriedHandcuffs--;
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(1f);
        _frontDesk.movingHandcuffs = false;
    }
}

