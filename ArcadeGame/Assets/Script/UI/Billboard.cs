using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        // 최적화를 위해 메인 카메라를 캐싱해 둡니다.
        mainCam = Camera.main;
    }

    // ⭐ 핵심: Update가 아니라 LateUpdate를 사용합니다.
    void LateUpdate()
    {
        if (mainCam == null) return;

        // 카메라의 회전값과 내 회전값을 완전히 똑같이 맞춥니다.
        transform.rotation = mainCam.transform.rotation;
    }
}
