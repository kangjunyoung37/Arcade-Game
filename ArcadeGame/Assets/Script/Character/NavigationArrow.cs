using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    [Header("목표물 및 설정")]
    public Transform target;       // 화살표가 가리킬 목적지
    public float rotateSpeed = 10f; // 화살표가 회전하는 부드러운 정도

    void Update()
    {
        if (target == null) return;
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetEnable(bool enable)
    {
        gameObject.SetActive(enable);
    }
}
