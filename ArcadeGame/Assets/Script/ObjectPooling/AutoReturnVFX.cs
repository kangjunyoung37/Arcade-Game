using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AutoReturnVFX : PoolAble
{
    private void OnParticleSystemStopped()
    {
        StartCoroutine(Realse());
    }

    IEnumerator Realse()
    {
        yield return new WaitForSeconds(1f);
        ReleaseObject();
    }
}
