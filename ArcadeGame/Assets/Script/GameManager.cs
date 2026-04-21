using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] private Character mainCharacter;
    [SerializeField] private MainCamera mainCamera;
    
    [Header("Update Lists")] 
    private List<IUpdateable> _players = new List<IUpdateable>();
    private List<IUpdateable> _enemies = new List<IUpdateable>();
    private List<IUpdateable> _bullets = new List<IUpdateable>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Update()
    {
        //UpdateList(_players, Time.deltaTime);
        UpdateList(_bullets, Time.deltaTime);
    }
    
    private void UpdateList(List<IUpdateable> list, float dt)
    {

        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i].OnTick(dt);
        }    
    }

    public void AddPlayer(IUpdateable player)
    {
         if(!_players.Contains(player)) _players.Add(player);
    }

    public void RemovePlayer(IUpdateable player)
    {
        _players.Remove(player);
    }

    public void AddBullet(IUpdateable bullet)
    {
        if (!_bullets.Contains(bullet)) _bullets.Add(bullet);
    }

    public void RemoveBullet(IUpdateable bullet)
    {
        _bullets.Remove(bullet);
    }
    public Character GetCharacter()
    {
        return mainCharacter;
    }
    
    public MainCamera GetMainCamera()
    {
        return mainCamera;
    }
    
}
