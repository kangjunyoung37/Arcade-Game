using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class UnlockZone : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image fillImage;
    
    [SerializeField] private Transform moneyPos;
    protected Character _player;
    
    [Header("가격 설정")]
    public int totalCost = 50;   // 총 가격
    private int paidMoney = 0;
    
    private bool isPlayerOnZone = false;

    private void Start()
    {
        _player = GameManager.Instance.GetCharacter();
        costText.text = totalCost.ToString();
        PopUpAppear();
    }

    protected abstract void Unlock();
    public virtual void Go()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnZone = true;
            StartCoroutine(PayRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnZone = false;
        }
    }

    private IEnumerator PayRoutine()
    {
        while (isPlayerOnZone && paidMoney < totalCost && _player.inventory.curMoney != 0)
        {
            // TODO: 실제 플레이어의 돈을 깎는 로직 호출 (예: Player.Instance.TryPay(1))
            // 여기서는 테스트를 위해 무조건 지불한다고 가정합니다.
            _player.inventory.UseMoney(moneyPos,Vector3.zero);
            
            paidMoney+=5;
            UpdateUI();
            // 0.1초마다 돈을 지불하는 연출 (속도 조절)
            yield return new WaitForSeconds(0.1f);
        }

        if (paidMoney >= totalCost)
        {
            Unlock();
            gameObject.SetActive(false);
            //Debug.Log("구매 완료! 아이템 스폰 또는 길을 엽니다.");
            // TODO: 구매 완료 로직 실행
        }
    }

    private void UpdateUI()
    {
        // 💡 핵심: fillAmount (0.0 ~ 1.0f)를 계산해서 업데이트합니다.
        float fillRatio = (float)paidMoney / totalCost;
        fillImage.fillAmount = fillRatio;

        // 남은 가격 텍스트 업데이트
        int remainingCost = totalCost - paidMoney;
        costText.text = remainingCost.ToString();
    }

    private void OnEnable()
    {
        PopUpAppear();
    }

    public void PopUpAppear()
    {
        transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.5f, 10, 1f);
    }
}
