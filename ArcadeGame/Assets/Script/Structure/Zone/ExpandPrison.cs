using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ExpandPrison :  UnlockZone
{

    [SerializeField] private GameObject _gameObject;
    private Sequence _currentScaleSequence;
    private float duration = 0.5f;
    
    protected override void Unlock()
    {
        GameManager.Instance.GetPrison().maxCapacity = 40;
        GameManager.Instance.GetPrison().UpdateUI();
        // 현재 크기는 (1,1,1)이어야 합니다.
        _gameObject.SetActive(true);
        
        _gameObject.transform.localScale = Vector3.one;
        // 새로운 시퀀스 생성
        _currentScaleSequence = DOTween.Sequence();
        // 1. duration의 50% 시간 동안 1.5배로 훅 커집니다. (Ease.OutQuad로 부드럽게 끝남)
        _currentScaleSequence.Append(_gameObject.transform.DOScale(Vector3.one * 1.5f, duration * 0.5f).SetEase(Ease.OutQuad));

        // 2. 이어서 duration의 60% 시간 동안 다시 1.0배로 쇽 돌아옵니다. (Ease.InQuad로 부드럽게 시작)
        _currentScaleSequence.Append(_gameObject.transform.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.InQuad));
    }
}
