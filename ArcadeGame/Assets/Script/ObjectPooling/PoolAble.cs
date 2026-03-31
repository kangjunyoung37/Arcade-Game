
using System;
using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

public class PoolAble : MonoBehaviour
{
    public IObjectPool<GameObject> Pool { get; set;}
    public float duration = 0.3f;
    public float overshoot = 3f;
    public float tossDuration = 0.6f; // 던지는 시간
    public float jumpPower = 2.0f;
    private Sequence _currentScaleSequence;
    private Sequence _moveSequence;
    public void ReleaseObject()
    {
        gameObject.transform.SetParent(null);
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.Euler(0,0,0);
        gameObject.transform.localScale = Vector3.one;
        Pool.Release(gameObject);
        transform.DOKill();
    }
    
    public void OnEnable()
    {
        
    }
    public void PopUpEffect()
    {
        // 현재 크기는 (1,1,1)이어야 합니다.
        transform.localScale = Vector3.one;
        // 새로운 시퀀스 생성
        _currentScaleSequence = DOTween.Sequence();
        // 1. duration의 50% 시간 동안 1.5배로 훅 커집니다. (Ease.OutQuad로 부드럽게 끝남)
        _currentScaleSequence.Append(transform.DOScale(Vector3.one * 1.5f, duration * 0.5f).SetEase(Ease.OutQuad));

        // 2. 이어서 duration의 60% 시간 동안 다시 1.0배로 쇽 돌아옵니다. (Ease.InQuad로 부드럽게 시작)
        _currentScaleSequence.Append(transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.InQuad));
    }
    

    public void DisAppear(float disappearTime, bool onlyDisappear = false)
    {
        if (_currentScaleSequence != null && _currentScaleSequence.IsActive())
        {
            _currentScaleSequence.Kill();
        }

        if (onlyDisappear)
        {
            transform.DOScale(Vector3.zero, disappearTime).SetEase(Ease.OutQuad);
        }
        else
        {
            transform.DOScale(Vector3.zero, disappearTime).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    gameObject.transform.SetParent(null);
                    ReleaseObject();
                });
        }

    }

    public void MoveToDirect(Transform target, Vector3 offSet, float time)
    {
        transform.SetParent(target);
        
        transform.DOLocalMove(offSet, time).SetEase(Ease.Linear).OnComplete(() =>
        {
            PopUpEffect();
        });
        
        transform.DOLocalRotate(new Vector3(0,0,0), time).SetEase(Ease.Linear);
    }
    
    public void MoveToTransform(Transform target, Vector3 offSet, bool disappear = false)
    {
        transform.SetParent(target);
        // 💡 핵심: DOJump를 사용하여 포물선 이동 구현
        // rock.transform.DOJump(
        //     Vector3 endValue,    // 목적지 (용광로 입구 월드 좌표)
        //     float jumpPower,     // 높이
        //     int numJumps,        // 점프 횟수 (던지기이므로 1회)
        //     float duration,      // 시간
        //     bool snapping = false // 좌표 정수화 (끄는 것이 부드럽습니다)
        // );
        Vector3 pos = offSet;
        if (disappear)
        {
            transform.DOLocalJump(pos,jumpPower, 1, tossDuration).OnComplete(() =>
            {
                ReleaseObject();
            });
            transform.DOLocalRotate(new Vector3(0,0,0), tossDuration).SetEase(Ease.OutQuad);
           
        }
            
        else
        {
            transform.DOLocalJump(pos,jumpPower, 1, tossDuration);
            transform.DOLocalRotate(new Vector3(0,0,0), tossDuration).SetEase(Ease.OutQuad);
            PopUpEffect();
        }

    }
}
