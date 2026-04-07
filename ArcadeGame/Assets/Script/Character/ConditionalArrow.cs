using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class ConditionalArrow : MonoBehaviour
{
    [Header("연결")]
    public Transform arrowModel; // 둥실거릴 자식 화살표 모델
    public List<Transform> targetPoints; // 이동할 목표 지점들 리스트

    [Header("설정")]
    public float moveSpeed = 5f;     // 이동 속도
    public float hoverRange = 0.5f;  // 위아래 이동 범위
    public float hoverDuration = 1f; // 둥실 한 번 하는 데 걸리는 시간

    private int currentIndex = 0; // 현재 화살표가 있는 목표 지점의 번호(인덱스)
    private Tween moveTween;      // 이동 애니메이션을 관리할 변수
    private bool _isStop = false;
    void Start()
    {
        // 1. 자식 모델 무한 둥실둥실 (부모의 이동과 상관없이 계속 실행됨)
        if (arrowModel != null)
        {
            arrowModel.DOLocalMoveY(hoverRange, hoverDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // 2. 시작할 때 첫 번째 지점(0번)으로 위치를 맞춰줍니다.
        if (targetPoints.Count > 0 && targetPoints[0] != null)
        {
            transform.position = targetPoints[0].position;
        }
    }

    // ✨ 핵심: 조건이 달성되었을 때 다른 스크립트에서 이 함수를 호출하게 합니다!
    public void MoveToNextPoint()
    {
        Debug.Log("MoveToNextPoint");
        // 아직 가야 할 다음 지점이 남아있는지 확인합니다.
        if (currentIndex < targetPoints.Count - 1)
        {
            currentIndex++; // 목표 번호를 다음 번호로 올림 (+1)
            Transform target = targetPoints[currentIndex]; // 다음 목표 지점 가져오기
            GameManager.Instance.GetCharacter().navArrow.SetTarget(target);

            moveTween?.Kill();

            // 거리 기반으로 이동 시간 계산 (일정한 속도로 날아가게 하기 위함)
            //float distance = Vector3.Distance(transform.position, target.position);
            //float duration = distance / moveSpeed;

            transform.position = target.position;
            // 다음 지점으로 부드럽게 날아갑니다!
            //moveTween = transform.DOMove(target.position, duration).SetEase(Ease.InOutQuad);
        }
        else
        {
            Debug.Log("모든 지점 안내가 끝났습니다!");
            // 💡 팁: 모든 안내가 끝났다면 화살표를 숨겨버리는 것도 좋습니다.
             gameObject.SetActive(false);
             GameManager.Instance.GetCharacter().navArrow.SetEnable(false);
             _isStop = true;
        }
    }

    public void MoveArrow(float duration)
    {
        if(!_isStop)
            StartCoroutine(MoveArrowC(duration));
    }
    IEnumerator MoveArrowC(float duration)
    {
        yield return new WaitForSeconds(duration);
        GameManager.Instance.GetArrow().MoveToNextPoint();
    }
}
