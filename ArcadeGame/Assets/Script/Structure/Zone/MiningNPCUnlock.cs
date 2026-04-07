using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningNPCUnlock : UnlockZone
{
    [SerializeField] private GameObject _miningNPC;
    [SerializeField] private PoliceUnlock _policeUnlock;
    protected override void Unlock()
    {
        _miningNPC.SetActive(true);
        _policeUnlock.gameObject.SetActive(true);
    }
}
