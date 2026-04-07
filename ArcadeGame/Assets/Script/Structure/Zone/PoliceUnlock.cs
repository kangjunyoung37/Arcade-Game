using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceUnlock : UnlockZone
{
    [SerializeField]  private GameObject _policeNPC;
    protected override void Unlock()
    {
        _policeNPC.gameObject.SetActive(true);
    }
}
