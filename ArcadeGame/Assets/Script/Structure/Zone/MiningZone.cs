using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningZone : MonoBehaviour
{
    private Character _player;

    private void Start()
    {
        _player = GameManager.Instance.GetCharacter();
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.GetArrow().MoveArrow(0.2f);
            _player.SwitchMineMode(true);
        }
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player.SwitchMineMode(false);
        }
    }
    
}
