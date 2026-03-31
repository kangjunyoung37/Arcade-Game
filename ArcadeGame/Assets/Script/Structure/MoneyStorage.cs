using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyStorage : MonoBehaviour
{
    [SerializeField]
    private int _curMoney = 0;
    [SerializeField]
    private List<Money> _moneyList = new List<Money>();
    public Transform moneyStackPoint;
    private Character _player;
    private float length = -0.4f;
    private float height = 0.15f;
    private float width = 0.72f;
    private bool firstTouch = false;
    private void Start()
    {
        _player = GameManager.Instance.GetCharacter();
    }
    public void GetMoney(Money money)
    {
        int lCnt,hCnt,wCnt;
        hCnt = _curMoney / 6;
        lCnt = _curMoney % 6 / 2;
        wCnt = _curMoney % 6 % 2;
        money.MoveToDirect(moneyStackPoint, new Vector3(wCnt*width,hCnt*height,lCnt*length), 0.2f);
        _moneyList.Add(money);
        _curMoney++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
            if (!firstTouch)
            {
                firstTouch = true;
                GameManager.Instance.GetArrow().MoveArrow(0.2f);
                Transform pos = GameManager.Instance.GetDrillUnlock().transform;
                GameManager.Instance.GetMainCamera().ShowEventPoint(pos,(() => pos.gameObject.SetActive(true)));
            }

            for (int i = 0; i < _moneyList.Count; i++)
            {
                _player.inventory.AddMoney(_moneyList[i]);
            }
            _moneyList.Clear();
            _curMoney = 0;
        }
        
    }
}
