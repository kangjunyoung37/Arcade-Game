using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Rock : PoolAble
{
    private Renderer _renderer;
    [Header("주괴 색상")]
    [SerializeField] private Color color;
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void ChangeColor(float time)
    {
        _renderer.material.color = color;
        _renderer.material.DOColor(Color.white, time);
    }
}
