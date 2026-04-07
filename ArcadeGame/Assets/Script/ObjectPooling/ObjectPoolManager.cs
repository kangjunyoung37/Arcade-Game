using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
[System.Serializable]
    private class ObjectInfo
    {
        public string objectName;
        public GameObject prefab;
        public int count;
    }
    public static ObjectPoolManager instance;
    
    public bool IsReady { get; private set; }
    [SerializeField] private ObjectInfo[] objectInfos;
    private string _objectName;
    private Dictionary<string,IObjectPool<GameObject>> _objectPoolDic = new Dictionary<string, IObjectPool<GameObject>>();
    private Dictionary<string, GameObject> _goDic = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        Init();
        
    }

    private void Init()
    {
        IsReady = false;

        for (int idx = 0; idx < objectInfos.Length; idx++)
        {
            IObjectPool<GameObject> pool = new ObjectPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool,
                OnDestroyPoolObject, true, objectInfos[idx].count, objectInfos[idx].count);
            if (_goDic.ContainsKey(objectInfos[idx].objectName))
            {
                Debug.LogFormat("{0} 이미 등록된 오브젝트입니다.", objectInfos[idx].objectName);
                return;
            }

            _goDic.Add(objectInfos[idx].objectName, objectInfos[idx].prefab);
            _objectPoolDic.Add(objectInfos[idx].objectName, pool);

            // 미리 오브젝트 생성 해놓기
            for (int i = 0; i < objectInfos[idx].count; i++)
            {
                _objectName = objectInfos[idx].objectName;
                PoolAble poolAbleGo = CreatePooledItem().GetComponent<PoolAble>();
                poolAbleGo.Pool.Release(poolAbleGo.gameObject);
            }
        }

        IsReady = true;
    }
    
    private GameObject CreatePooledItem()
    {
        GameObject poolGo = Instantiate(_goDic[_objectName]);
        poolGo.GetComponent<PoolAble>().Pool = _objectPoolDic[_objectName];
        return poolGo;
    }
    
    private void OnTakeFromPool(GameObject poolGo)
    {
        poolGo.SetActive(true);
    }
    
    private void OnReturnedToPool(GameObject poolGo)
    {
        poolGo.SetActive(false);
    }
    
    private void OnDestroyPoolObject(GameObject poolGo)
    {
        Destroy(poolGo);
    }

    public GameObject GetGo(string goName)
    {
        _objectName = goName;

        if (_goDic.ContainsKey(goName) == false)
        {
            //Debug.LogFormat("{0} 오브젝트풀에 등록되지 않은 오브젝트입니다.", goName);
            return null;
        }

        return _objectPoolDic[goName].Get();
    }
    
    public T GetGo<T>(string goName) where T : Component
    {
        // 1. 기존에 만들어둔 함수를 통해 일단 GameObject를 꺼내옵니다.
        GameObject go = GetGo(goName);

        if (go == null) return null;

        // 2. 꺼낸 게임오브젝트에서 원하는 컴포넌트를 찾아서 반환합니다.
        T component = go.GetComponent<T>();

        // (안전장치) 혹시라도 프리팹에 해당 스크립트를 안 붙여놨을 때를 대비한 경고문
        if (component == null)
        {
            Debug.LogError($"[오브젝트 풀] {goName} 프리팹에 {typeof(T).Name} 컴포넌트가 안 붙어있습니다!");
        }

        return component;
    }
}
