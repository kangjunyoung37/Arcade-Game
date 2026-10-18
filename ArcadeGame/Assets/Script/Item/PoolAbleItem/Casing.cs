using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Casing : PoolAble
{
    [Header("Ejection Settings")] public float minForce = 3f;
    public float maxForce = 5f;
    public float spinForce = 10f;
    public float lifeTime = 3f;
    private float _remainingLifetime;
    private bool _isEjected;

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
        _remainingLifetime = Mathf.Max(0f, lifeTime);
        _isEjected = true;
    }

    private void Update()
    {
        if(!_isEjected) return;
        _remainingLifetime -= Time.deltaTime;
        if(_remainingLifetime > 0f) return;
        _isEjected = false;
        if(Pool != null) ReleaseObject();
        else gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        _isEjected = false;
        _remainingLifetime = 0f;
    }

}
