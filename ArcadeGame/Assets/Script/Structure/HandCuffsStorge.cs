using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCuffsStorge : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;
    [SerializeField] private float height = 0.2f;
    
    [NonSerialized] public int _cnt = 0;
    
    [NonSerialized] public Stack<Handcuffs> handcuffsStack = new Stack<Handcuffs>();
    private Character _player;

    private void Start()
    {
        _player = GameManager.Instance.GetCharacter();
    }

    public void StackHandcuffs(Handcuffs handcuffs)
    {
        
        handcuffsStack.Push(handcuffs);
        Vector3 pos = stackPoint.position + (Vector3.up * (height * _cnt));
        handcuffs.transform.position = pos;
        handcuffs.PopUpEffect();
        _cnt++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.GetArrow().MoveArrow(0.2f);
            StartCoroutine(GiveHandcuffs());
        }
    }

    IEnumerator GiveHandcuffs()
    {
        while (true)
        {
            if(handcuffsStack.Count == 0)
                yield break;
            Handcuffs handcuffs = handcuffsStack.Pop();
            _player.inventory.AddHandcuffs(handcuffs);
            yield return new WaitForSeconds(0.05f);
            _cnt--;
        }
    }
}
