using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Header("레일 이동 속도")]
    public float scrollSpeed = 0.0f;

    public float originalSpeed = 8.0f;
    // X축으로 움직일지, Y축으로 움직일지 결정
    public bool moveX = false; 
    public bool moveY = true;

    
    private Material _railMaterial;
    private Vector2 _currentOffset;
    [Header("포인트")]
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private Transform workPoint;

    [SerializeField] private Transform pressPoint;

    [SerializeField] private Transform endPoint;
    
    [Header("수갑 저장고")]
    [SerializeField] private HandCuffsStorge handcuffsStore;
    
    //만들 횟수
    public int createCnt = 0;
    void Start()
    {
        // 내 오브젝트에 있는 렌더러에서 매테리얼(재질)을 가져옵니다.
        _railMaterial = GetComponent<Renderer>().material;
        StartCoroutine(MoveIron());
    }

    void Update()
    {
        // 1. 시간에 속도를 곱해서 이동할 거리를 계산합니다.
        float offset = Time.time * scrollSpeed;

        // 2. 방향에 맞춰 Vector2 값을 만듭니다.
        float x = moveX ? offset : 0;
        float y = moveY ? offset : 0;
        _currentOffset = new Vector2(x, y);

        // 3. 매테리얼의 텍스처 위치(Offset)를 밀어냅니다.
        _railMaterial.mainTextureOffset = _currentOffset;
    }
    public void SetBeltActive(bool isActive) {
        scrollSpeed = isActive ? originalSpeed : 0f;
    }

    IEnumerator MoveIron()
    {
        while (true)
        {
            if (createCnt > 0)
            {
                
                SetBeltActive(true);
                createCnt--;
                GameObject iron = ObjectPoolManager.instance.GetGo("Rock");
                Rock rock =  iron.GetComponent<Rock>();
                iron.transform.position = spawnPoint.position;
                iron.transform.rotation = Quaternion.identity;
                iron.transform.localScale = new Vector3(0.7f,0.7f,0.7f);
                rock.ChangeColor(0.4f);
                //철 주괴 움직이기
                iron.transform.DOMove(workPoint.transform.position, 0.4f).SetEase(Ease.Linear);
                yield return new WaitForSeconds(0.4f);
                float originalY = pressPoint.position.y;
                //벨트 멈추고 프레스 내리기
                SetBeltActive(false);
                pressPoint.DOMoveY(workPoint.transform.position.y, 0.2f);
                yield return new WaitForSeconds(0.2f);
                
                rock.ReleaseObject();
                var cuffs = ObjectPoolManager.instance.GetGo("HandCuffs");
                Handcuffs handcuffs = cuffs.GetComponent<Handcuffs>();
                
                cuffs.transform.position = workPoint.position;
                if (createCnt == 0)
                    SetBeltActive(true);
                SetBeltActive(true);
                pressPoint.DOMoveY(originalY, 0.4f).SetEase(Ease.OutQuad);
                
                cuffs.transform.DOMove(endPoint.position, 0.4f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    StartCoroutine(StorageHandCuffs(handcuffs));
                });
                
            }

            yield return null;
        }
    }

    IEnumerator StorageHandCuffs(Handcuffs hand)
    {
        hand.DisAppear(0.1f,true);
        yield return new WaitForSeconds(0.1f);
                
        handcuffsStore.StackHandcuffs(hand);
        if(createCnt == 0)
            SetBeltActive(false);
    }
    
}