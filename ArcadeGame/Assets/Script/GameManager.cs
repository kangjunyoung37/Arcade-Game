using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] private Character mainCharacter;
    [SerializeField] private Furnace furnace;
    [SerializeField] private FrontDesk frontDesk;
    [SerializeField] private HandCuffsStorge handcuffsStorge;
    [SerializeField] private Prison prison;
    [SerializeField] private DrillUnlock drillUnlock;
    [SerializeField] private MainCamera mainCamera;
    [SerializeField] private ConditionalArrow arrow;
    [Header("죄수 스폰 포인트")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("죄수 감옥 이동 경로")]
    [SerializeField] public List<Transform> moveToPrisonPoints = new List<Transform>();
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

    private void Start()
    {
        StartCoroutine(SpawnPrisoner());
    }
    public Character GetCharacter()
    {
        return mainCharacter;
    }

    public Furnace GetFurnace()
    {
        return furnace;
    }

    public FrontDesk GetFrontDesk()
    {
        return frontDesk;
    }

    public Prison GetPrison()
    {
        return prison;
    }

    public DrillUnlock GetDrillUnlock()
    {
        return drillUnlock;
    }
    public HandCuffsStorge GetHandcuffsStorge()
    {
        return handcuffsStorge;
    }

    public MainCamera GetMainCamera()
    {
        return mainCamera;
    }

    public ConditionalArrow GetArrow()
    {
        return arrow;
    }
    IEnumerator SpawnPrisoner()
    {
        while (true)
        {
            if (frontDesk.prisoners.Count < 4)
            {
                var npc = ObjectPoolManager.instance.GetGo("Prisoner");
                npc.transform.position = spawnPoint.position;
                Prisoner prisoner = npc.GetComponent<Prisoner>();
                prisoner.Init();
                frontDesk.prisoners.Add(prisoner);
                
            }

            yield return new WaitForSeconds(0.1f);
        }
        
    }
}
