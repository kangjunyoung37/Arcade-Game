using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Casing : PoolAble
{
    [Header("Ejection Settings")] public float minForce = 3f;
    public float maxForce = 5f;
    public float spinForce = 10f;
    public float lifeTime = 3f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    
    public void Eject(Vector3 ejectDirection)
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        var randomForce = Random.Range(minForce, maxForce);
        
        _rb.AddForce(ejectDirection * randomForce, ForceMode.Impulse);
        var randomSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * spinForce;
       
        _rb.AddTorque(randomSpin, ForceMode.Impulse);
        StartCoroutine(DeactivateAfterTime());
    }

    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReleaseObject();
    }

}
