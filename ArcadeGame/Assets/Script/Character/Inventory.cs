using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("인벤토리 설정")]
    public int maxMineral = 10;
    public int curMineral = 0;
    public int curHandcuffs = 0;
    public int curMoney = 0;
    
    //등에 돈 쌓는 용도
    private int _curStackMoney = 0;
    [Header("시각적 연출")]
    public Transform mineralsParent; // 등에 돌이 쌓일 위치
    public Transform moneyParent;
    public Transform handcuffsParent; // 앞에 수갑이 쌓일 위치
    public float height = 0.2f;
    private Stack<Rock> _rocks = new Stack<Rock>(); // 쌓인 돌 관리
    private Stack<Handcuffs> _handcuffs = new Stack<Handcuffs>();
    private Stack<Money> _moneys = new Stack<Money>();
    [Header("돈 UI")] 
    [SerializeField] TMP_Text moneyText;
    // 캐릭터가 광물을 캤을 때 호출하는 함수
    public void AddRock()
    {
        if (curMineral >= maxMineral) 
        {
            Debug.Log("가방이 꽉 찼습니다!");
            return; // 꽉 찼으면 더 이상 생성 안 함
        }
        
        // 시각적 돌 생성 및 배치 (Object Pooling)
        var rock = ObjectPoolManager.instance.GetGo("Rock").GetComponent<Rock>();
        rock.transform.SetParent(mineralsParent);
        rock.transform.localPosition = new Vector3(0, height * curMineral, 0);
        rock.transform.localRotation = Quaternion.identity;
        rock.PopUpEffect();
        curMineral++;
        
        _rocks.Push(rock);
        Debug.Log($"현재 돌 개수: {curMineral} / {maxMineral}");
    
    }
    
    // 나중에 용광로에 돌을 넣을 때 쓸 함수도 여기에 미리 만들어둡니다.
    public Rock MoveRock(Transform target, Vector3 offSet)
    {
        // Object Pool 반환 및 curMineral 감소 로직...
        if (curMineral == 0)
            return null;
        curMineral--;
        Rock rock = _rocks.Pop();
        rock.MoveToTransform(target,offSet);

        return rock;

    }

    public void AddHandcuffs(Handcuffs handcuffs)
    {
        _handcuffs.Push(handcuffs);
        handcuffs.MoveToTransform(handcuffsParent,(Vector3.up * (height*curHandcuffs)));
        curHandcuffs++;
    }

    public Handcuffs MoveHandcuffs(Transform target, Vector3 offSet)
    {
        if (curHandcuffs == 0)
            return null;
        curHandcuffs--;
        Handcuffs handcuffs = _handcuffs.Pop();
        handcuffs.MoveToTransform(target,offSet);
        return handcuffs;
    }

    public void AddMoney(Money money)
    {
        curMoney+=5;
        UpdateUI();
        _moneys.Push(money);
        money.MoveToDirect(moneyParent,(Vector3.up * (height*_curStackMoney)),0.2f);
        _curStackMoney++;
    }

    public void UseMoney(Transform target, Vector3 offSet)
    {
        if (_curStackMoney == 0)
            return;
        _curStackMoney--;
        curMoney -= 5;
        Money money = _moneys.Pop();
        money.MoveToTransform(target,offSet,true);
        UpdateUI();
    }

    private void UpdateUI()
    {
        moneyText.text = curMoney.ToString();
    }
}