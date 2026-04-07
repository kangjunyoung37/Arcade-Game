using System.Collections;
using UnityEngine;

public class DrillTool : MiningTool
{
    [Header("드릴 설정")]
    public Transform drillBit; // 빙글빙글 도는 드릴 날
    public float spinSpeed = 1000f; // 회전 속도
    public float drilltime = 0.3f; // 0.1초마다 채광 데미지 들어감

    private float timer = 0f;
    private bool _drilling = false;
    public override void Mine()
    {
        GameManager.Instance.GetCharacter().MiningWithTool();
        if (_drilling)
        {
            timer = 0;
        }
        else
        {
            StartCoroutine(SpinDrill());
        }
        
        
    }

    IEnumerator SpinDrill()
    {
        _drilling = true;
        while (timer <= drilltime)
        {
            // 1. 드릴 날을 Z축(혹은 알맞은 축) 기준으로 빙글빙글 돌린다 (애니메이션 대체)
            drillBit.Rotate(Vector3.forward * (spinSpeed * Time.deltaTime));
            
            timer += Time.deltaTime;

            yield return null;
        }

        timer = 0;
        _drilling = false;
    }
}