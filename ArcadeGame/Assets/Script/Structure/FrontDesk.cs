using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UIElements;

public class FrontDesk : MonoBehaviour
{
    
    [Header("쌓을 장소")]
    public Transform stackPoint;
    public Transform waitingPos;
    
    [Header("데스크 데이터")] 
    [SerializeField] private float height = 0.2f;

    [NonSerialized] public int curHandcuffs = 0;
    [NonSerialized] public Stack<Handcuffs> handcuffsStack = new Stack<Handcuffs>();
    private Character _player;
    public bool movingHandcuffs = false;
    [Header("죄수")]
    public List<Prisoner> prisoners = new List<Prisoner>();

    [Header("돈 저장고")] 
    public MoneyStorage moneyStorage;
    
    private void Start()
    {
        _player = GameManager.Instance.GetCharacter();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.GetArrow().MoveArrow(0.2f);
            StartCoroutine(GetHandcuffs());
        }
    }

    IEnumerator GetHandcuffs()
    {
        movingHandcuffs = true;
        while (_player.inventory.curHandcuffs != 0)
        {
            Handcuffs handcuffs = _player.inventory.MoveHandcuffs(stackPoint,new Vector3(0, height * curHandcuffs, 0));
            handcuffsStack.Push(handcuffs);
            curHandcuffs++;
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(1f);
        movingHandcuffs = false;
    }

    public void GiveHandcuffs()
    {
        if (handcuffsStack.Count > 0)
        {
            Handcuffs hc = handcuffsStack.Pop();
            curHandcuffs--;
            hc.MoveToTransform(prisoners[0].transform,Vector3.zero,true);
        }
    }

    public void SendPrisoner()
    {
        int cnt = prisoners[0].pricehandCuffs;
        
        for (int i = 0; i < cnt; i++)
        {
            Money money;
            var mn = ObjectPoolManager.instance.GetGo("Money");
            money = mn.GetComponent<Money>();
            money.transform.position = prisoners[0].transform.position;
            moneyStorage.GetMoney(money);
        }
        prisoners.RemoveAt(0);
    }
}
