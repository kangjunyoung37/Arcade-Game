using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class MainCamera : MonoBehaviour
{
    public Transform target;           // 플레이어
    public Vector3 offset = new Vector3(0f, 10f, -7f); // 쿼터뷰 오프셋
    public float smoothSpeed = 5f;
    private bool isFollowing = true;
    void Start()
    {
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }
    void LateUpdate()
    {
        if (!isFollowing || target == null) return;
        Vector3 desiredPos = target.position + offset;
        //transform.position = desiredPos;
        //transform.LookAt(target); // 항상 플레이어를 바라봄
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
    
    public void ShowEventPoint(Transform targetPoint, Action callbackFunction, float moveTime = 1f, float stayTime = 1.5f)
    {
        if (targetPoint == null) return;

        // 1. 플레이어 추적 스위치를 끕니다.
        isFollowing = false;

        // 2. 목표를 비출 때 카메라가 있어야 할 위치
        // (현재 쿼터뷰 각도를 그대로 유지하기 위해 offset을 더해줍니다!)
        Vector3 eventCameraPos = targetPoint.position + offset;

        // 3. DOTween 시퀀스(연속 동작) 생성
        Sequence camSeq = DOTween.Sequence();

        // 동작 A: 목표 지점으로 부드럽게 날아갑니다. (Ease.InOutCubic은 출발/도착이 부드러운 효과)
        camSeq.Append(transform.DOMove(eventCameraPos, moveTime).SetEase(Ease.InOutCubic));
        
        camSeq.AppendCallback(() =>
        {
            callbackFunction?.Invoke(); 
        });
       
        // 동작 B: 도착해서 구경할 시간(stayTime)만큼 대기합니다.
        camSeq.AppendInterval(stayTime);
        
        // 동작 C: 연출이 끝났을 때의 처리
        camSeq.OnComplete(() => 
        {
            isFollowing = true; 
        });
    }
}