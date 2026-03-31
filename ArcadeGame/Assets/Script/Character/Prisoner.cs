using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Prisoner : PoolAble
{
    [SerializeField] private int needHandCuffs = 4;
    [NonSerialized] public int pricehandCuffs = 0; 
    [Header("이동 설정")]
    public float moveSpeed = 8f; // 걷는 속도
    public float waitingDistance = 3.0f;
    
    [Header("UI")]
    [SerializeField] private TMP_Text needText;
    [SerializeField] private Billboard _billboard;
    // 에디터에서 순서대로 도착할 빈 게임오브젝트들을 넣어줍니다.
    private List<Transform> _waypoints; 

    private Tween _pathTween;
    private Tween _moveTween;
    
    private int _waitingNum = -1;
    private bool _isWalking = false;
    private void Start()
    {
        _waypoints = GameManager.Instance.moveToPrisonPoints;
        StartCoroutine(Wait());
        
    }
    
    public void Init()
    {
        needHandCuffs = UnityEngine.Random.Range(2,4);
        needText.text = needHandCuffs.ToString();
        pricehandCuffs = needHandCuffs;
    }
    // 외부에서 "출발해!" 라고 명령할 때 부르는 함수
    private void StartPatrol()
    {
        // 웨이포인트가 세팅되어 있지 않으면 작동안함
        if (_waypoints == null || _waypoints.Count == 0) return;

        // 1. Transform 배열을 Vector3(좌표) 배열로 변환합니다. (DOPath가 요구하는 양식)
        Vector3[] pathPositions = new Vector3[_waypoints.Count];
        for (int i = 0; i < _waypoints.Count; i++)
        {
            pathPositions[i] = _waypoints[i].position;
        }

        // 2. 전체 이동 거리 계산 (속도를 처음부터 끝까지 일정하게 유지하기 위함)
        float totalDistance = 0f;
        Vector3 currentPos = transform.position;
        foreach (Vector3 wp in pathPositions)
        {
            totalDistance += Vector3.Distance(currentPos, wp);
            currentPos = wp;
        }

        // 3. 전체 걸리는 시간 = 전체 거리 ÷ 내 속도
        float duration = totalDistance / moveSpeed;

        // 4. 기존에 걷고 있었다면 취소
        if (_pathTween != null && _pathTween.IsActive())
        {
            _pathTween.Kill();
        }

        //  5. DOPath 실행
        _pathTween = transform.DOPath(
                pathPositions,   // 거쳐갈 좌표들 배열
                duration,        // 전체 걸리는 시간
                PathType.Linear  // 꺾이는 방식 (Linear: 직선으로 딱딱 꺾임)
            )
            .SetLookAt(0.01f)    //  걷는 방향(앞)을 자동으로 바라보게 만듭니다!
            .SetEase(Ease.Linear)// 가속/감속 없이 일정한 걷기 속도 유지
            .OnComplete(() =>
            {
                //Debug.Log("최종 목적지 도착 완료!");
                // 애니메이터를 조종해서 Idle(대기) 상태나 작업 상태로 변경
            });
    }
    

    // 외부에서 목적지를 지시할 때 부르는 함수
    public void WalkTo(Vector3 targetPoint)
    {
        // 1. 기존에 이동 중이던 명령이 있다면 즉시 취소합니다.
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
        }

        _isWalking = true;
        // 2. 현재 위치와 목적지 사이의 '거리'를 구합니다.
        float distance = Vector3.Distance(transform.position, targetPoint);

        // 3. 거리 ÷ 속도 = 걸리는 시간(Duration) 계산
        float duration = distance / moveSpeed;

        // 4. 자연스럽게 목적지 방향으로 고개 돌리기 (0.2초 만에 휙 돕니다)
        // AxisConstraint.Y를 넣어야 언덕을 볼 때 몸이 뒤로 눕지 않습니다!
        transform.DOLookAt(targetPoint, 0.2f, AxisConstraint.Y);

        // 5. 계산된 시간 동안 일정한 속도(Linear)로 걸어갑니다.
        _moveTween = transform.DOMove(targetPoint, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => 
            {
                //Debug.Log("목적지 도착! (노동 시작 등 다음 행동 연결)");
                // 예: 노동 애니메이션 재생
                _isWalking = false;
            });
    }

    IEnumerator Wait()
    {
        FrontDesk desk = GameManager.Instance.GetFrontDesk();
        
        while (_waitingNum != 0 || _isWalking)
        {
            if (_waitingNum != desk.prisoners.IndexOf(this))
            {
                _waitingNum = desk.prisoners.IndexOf(this);
                Vector3 pos = desk.waitingPos.position - new Vector3(waitingDistance * _waitingNum ,0,0);
                WalkTo(pos);
            }

            yield return new WaitForSeconds(0.5f);
        }
        _billboard.gameObject.SetActive(true);
        StartCoroutine(GetHandCuffs());
    }

    IEnumerator GetHandCuffs()
    {
        FrontDesk desk = GameManager.Instance.GetFrontDesk();
        while (needHandCuffs != 0)
        {
            
            if (desk.curHandcuffs > 0 && !desk.movingHandcuffs)
            {
                needHandCuffs--;
                UpdateUI();
                desk.GiveHandcuffs();
                
            }
            yield return new WaitForSeconds(0.1f);
        }
        _billboard.gameObject.SetActive(false);
        desk.SendPrisoner();
        if (GameManager.Instance.GetPrison().CheckCanIn())
        {
            GameManager.Instance.GetPrison().realCapacity++;
            StartPatrol();
           
        }
        else
        {
            WalkTo(_waypoints[0].position);
        }
    }
    
    private void UpdateUI()
    {
        needText.text = needHandCuffs.ToString();
    }
}
