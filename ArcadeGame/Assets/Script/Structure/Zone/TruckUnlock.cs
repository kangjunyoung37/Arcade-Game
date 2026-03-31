using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckUnlock : UnlockZone
{
    protected override void Unlock()
    {
        _player.ChangeMineTool(2);
    }
}
