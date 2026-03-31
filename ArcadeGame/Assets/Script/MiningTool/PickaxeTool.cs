using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class PickaxeTool : MiningTool
{
    [Header("곡괭이 설정")]
    public Transform pickaxePivot; // 회전축
    public Vector3 startAngle = Vector3.zero;
    public Vector3 hitAngle = new Vector3(90, 0, 0);
    public float swingSpeed = 6f;
    public float miningCooldown = 0.5f;
    [SerializeField] private bool isSwinging = false;
    [SerializeField] private bool isPlayer = true;
    [SerializeField] private MiningNPC miningNpc;
    public override void Mine()
    {
        if (isSwinging)
            return;
        StartCoroutine(isPlayer ? SwingRoutine() : SwingRoutineNpc());
    }
    private IEnumerator SwingRoutine()
    {
        while (true)
        {
            if(!GameManager.Instance.GetCharacter().MineralCheck())
                yield break;
            isSwinging = true;
            Quaternion startRot = Quaternion.Euler(startAngle);
            Quaternion hitRot = Quaternion.Euler(hitAngle);
            
            // 1. 내려치기
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * swingSpeed;
                pickaxePivot.localRotation = Quaternion.Lerp(startRot, hitRot, t);
                yield return null;
            }

            // 2. 타격 순간! 
            GameManager.Instance.GetCharacter().MiningWithTool();

            // 3. 들어 올리기
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * swingSpeed;
                pickaxePivot.localRotation = Quaternion.Lerp(hitRot, startRot, t);
                yield return null;
            }

            // 4. 쿨다운 후 다시 스윙 가능
            yield return new WaitForSeconds(miningCooldown);
            
            isSwinging = false;
        }
        
    }

    private IEnumerator SwingRoutineNpc()
    {

        int cnt = 0;
        while (miningNpc.targetMineral != null && miningNpc.targetMineral.isAvailable)
        {
            isSwinging = true;
            Quaternion startRot = Quaternion.Euler(startAngle);
            Quaternion hitRot = Quaternion.Euler(hitAngle);
            miningNpc.PauseMove();
            // 1. 내려치기
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * swingSpeed;
                pickaxePivot.localRotation = Quaternion.Lerp(startRot, hitRot, t);
                yield return null;
            }
            
            if (cnt == 1)
            {
                miningNpc.MiningWithTool();
            }
            // 2. 타격 순간! 
            
            cnt++;
            // 3. 들어 올리기
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * swingSpeed;
                pickaxePivot.localRotation = Quaternion.Lerp(hitRot, startRot, t);
                yield return null;
            }

            // 4. 쿨다운 후 다시 스윙 가능
            yield return new WaitForSeconds(miningCooldown);
            
            isSwinging = false;
        }
        miningNpc.PlayMove();
    }

    private void OnDisable()
    {
        isSwinging = false;
    }
}