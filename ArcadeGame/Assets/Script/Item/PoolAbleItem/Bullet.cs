using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : PoolAble , IUpdateable
{
    [Header("Bullet Settings")]
    public float speed = 30.0f;
    
    private TrailRenderer _trail;
    private MeshRenderer _meshRenderer;
    private Collider _collider;
    private bool _isDead = false;
    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        GameManager.Instance.AddBullet(this);
    }
    private void OnDisable()
    {
        _trail.emitting = false;
        GameManager.Instance.RemoveBullet(this);
    }
    
    public void OnTick(float deltaTime)
    {
        BulletMove(deltaTime);
    }

    private void BulletMove(float deltaTime)
    {
        if (_isDead) return;
        transform.position += transform.forward * (speed * deltaTime);
    }

    public void Init()
    { 
        _trail.emitting = true;
        _trail.Clear();
        _isDead = false;
        _collider.enabled = true;
        _meshRenderer.enabled = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        _isDead = true;
        _collider.enabled = false;
        _trail.emitting = false;
        _meshRenderer.enabled = false;
        StartCoroutine(DelayDisable(_trail.time));
    }

    IEnumerator DelayDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseObject();
    }
}
 