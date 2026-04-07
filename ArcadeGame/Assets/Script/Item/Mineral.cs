using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using DG.Tweening;
using UnityEngine;

public class Mineral : MonoBehaviour
{
    public float respawnTime = 5f;
    public int mineralAmount = 1;
    public bool isAvailable = true;
    
    private MeshRenderer _meshRenderer;
    private Collider _col;
    private Character _mainCharacter;
    private Sequence _currentScaleSequence;


    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _col = GetComponent<Collider>();
        _mainCharacter = GameManager.Instance.GetCharacter();
    }
    
    IEnumerator Respawn()
    {
        // 광물 비활성화h
        _col.enabled = false;
        _meshRenderer.enabled = false;
        isAvailable = false;
        yield return new WaitForSeconds(respawnTime);
        // 리스폰
        _meshRenderer.enabled = true;
        RespawnEffect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MiningTool"))
        {
            if (isAvailable)
            {
                _mainCharacter.AddMineral(this);
                _mainCharacter.miningTool.Mine();
            }

        }
        else if (other.CompareTag("NPCMiningTool"))
        {
            MiningNPC miningNPC = other.gameObject.GetComponentInParent<MiningNPC>();
            if (miningNPC.targetMineral != null)
                return;
            miningNPC.targetMineral = this;
            miningNPC.miningTool.Mine();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MiningTool"))
        {
            _mainCharacter.RemoveMineral(this);
        }
        else if (other.CompareTag("NPCMiningTool"))
        {
            MiningNPC miningNPC = other.gameObject.GetComponentInParent<MiningNPC>();
            miningNPC.targetMineral = null;
        }
    }
    public void Mine()
    {
        GameObject go = ObjectPoolManager.instance.GetGo("Spark");
        ParticleSystem fx = go.GetComponent<ParticleSystem>();
        go.transform.position = transform.position + Vector3.up ;
        fx.Play();
        StartCoroutine(Respawn());
    }

    public void RespawnEffect()
    {
        // 현재 크기는 (1,1,1)이어야 합니다.
        transform.localScale = Vector3.one;
        // 새로운 시퀀스 생성
        _currentScaleSequence = DOTween.Sequence();
        // 1. duration의 50% 시간 동안 1.5배로 훅 커집니다. (Ease.OutQuad로 부드럽게 끝남)
        _currentScaleSequence.Append(transform.DOScale(new Vector3(1,0.2f,1), 0.2f).SetEase(Ease.OutQuad));
        _currentScaleSequence.AppendInterval(0.2f);
        // 2. 이어서 duration의 60% 시간 동안 다시 1.0배로 쇽 돌아옵니다. (Ease.InQuad로 부드럽게 시작)
        _currentScaleSequence.Append(transform.DOScale(new Vector3(1,1,1),0.2f).SetEase(Ease.InQuad)).OnComplete(() =>
        {
            isAvailable = true;
            _col.enabled = true;
        });
    }
}
