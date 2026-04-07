using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Furnace : MonoBehaviour
{
    Stack<Rock> _rocks = new Stack<Rock>();
    [Header("저장고")]
    [SerializeField] private Transform storage;
    public int curMineral = 0;
    public float height = 0.2f;
    public float width = 0.2f;
    public float length = 0.2f;
    
    [Header("벨트")]
    [SerializeField] private ConveyorBelt conveyorBelt;
    
    private Coroutine _rockCoroutine;
    private Character _player;

   
    public void Start()
    {
        _player = GameManager.Instance.GetCharacter();
        StartCoroutine(UseRock());
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.GetArrow().MoveArrow(0.2f);
            if (_rockCoroutine != null)
            {
                StopCoroutine(_rockCoroutine);
            }
            _rockCoroutine = StartCoroutine(GetRock());
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_rockCoroutine != null)
            {
                StopCoroutine(_rockCoroutine);
                _rockCoroutine = null;
            }
        }
    }

    IEnumerator GetRock()
    {
        while (true)
        {
            if(_player.inventory.curMineral == 0)
                yield break;
            int hCnt = curMineral / 2;
            int lCnt = curMineral % 2;
            Rock rock = _player.inventory.MoveRock(storage, new Vector3( width , height * hCnt, length * lCnt));
            curMineral++;
            _rocks.Push(rock);
            
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void GetRock(Rock rock)
    {
        int hCnt = curMineral / 2;
        int lCnt = curMineral % 2;
        rock.MoveToTransform(storage, new Vector3( width , height * hCnt, length * lCnt));
        curMineral++;
        _rocks.Push(rock);
    }
    IEnumerator UseRock()
    {
        while (true)
        {
            if (_rocks.Count > 0)
            {
                curMineral--;
                Rock rock = _rocks.Pop();
                rock.DisAppear(0.2f);
                conveyorBelt.createCnt++;
                
            }
            yield return new WaitForSeconds(1f);
        }
    }
}
