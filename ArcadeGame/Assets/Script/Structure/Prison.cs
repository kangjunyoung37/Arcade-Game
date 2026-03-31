using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Prison : MonoBehaviour
{
    [Header("감옥 데이터")]
    public int maxCapacity = 1;
    public int curCapacity = 0;
    public int realCapacity = 0;
    public GameObject expandZone;
    [SerializeField] private TMP_Text capacityText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Prisoner"))
        {
            curCapacity++;
            UpdateUI();
            if (curCapacity == maxCapacity)
            {
                
                GameManager.Instance.GetMainCamera().ShowEventPoint(transform,()=>{expandZone.SetActive(true);});
            }
        }
    }

    public bool CheckCanIn()
    {
        return realCapacity < maxCapacity;
    }

    public void UpdateUI()
    {
        capacityText.text = (curCapacity + " / " + maxCapacity).ToString();
    }
}
