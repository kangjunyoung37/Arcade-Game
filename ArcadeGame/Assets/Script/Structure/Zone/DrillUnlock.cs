using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillUnlock : UnlockZone
{
    [SerializeField] TruckUnlock _truckUnlock;
    [SerializeField] MiningNPCUnlock _miningNPCUnlock;
    protected override void Unlock()
    {
        Debug.Log("DrillUnlock");
        _player.ChangeMineTool(1);
        _miningNPCUnlock.gameObject.SetActive(true);
        _truckUnlock.gameObject.SetActive(true);
        
    }

   
}
