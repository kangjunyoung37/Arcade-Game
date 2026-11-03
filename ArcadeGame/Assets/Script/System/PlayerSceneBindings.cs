using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSceneBindings : MonoBehaviour
{
    private const int MemberCount = 2;
    private const float MemberWeight = 1f;
    private const float MemberRadius = 0f;

    [Header("Scene References")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Crosshair crosshair;
    [SerializeField] private Transform aimCameraTarget;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private CinemachineTargetGroup targetGroup;

    private readonly CinemachineTargetGroup.Target[] _members = new CinemachineTargetGroup.Target[MemberCount];

    public bool HasValidReferences => gameplayCamera != null && crosshair != null && aimCameraTarget != null && virtualCamera != null && targetGroup != null;

    public bool TryBind(Character player)
    {
        if (player == null || !HasValidReferences)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(
                "PlayerSceneBindings의 씬 참조를 모두 연결해 주세요.",
                this);
#endif
            return false;
        }

        if (!player.InitializeSceneReferences(gameplayCamera,crosshair, aimCameraTarget))
            return false;
        

        // 플레이어 → UI뿐 아니라 UI → 플레이어도 연결합니다.
        crosshair.player = player;

        aimCameraTarget.position = player.transform.position;

        // 현재 씬의 기존 구성:
        // 플레이어 + 마우스 조준 방향의 카메라 타깃
        _members[0] = new CinemachineTargetGroup.Target
        {
            target = player.transform,
            weight = MemberWeight,
            radius = MemberRadius
        };

        _members[1] = new CinemachineTargetGroup.Target
        {
            target = aimCameraTarget,
            weight = MemberWeight,
            radius = MemberRadius
        };

        targetGroup.m_Targets = _members;

        virtualCamera.Follow = targetGroup.transform;
        virtualCamera.LookAt = targetGroup.transform;

        // 스폰 위치로 그룹 중심을 즉시 갱신합니다.
        targetGroup.DoUpdate();

        // 이전 타깃 위치의 카메라 보간 상태를 사용하지 않습니다.
        virtualCamera.PreviousStateIsValid = false;

        return true;
    }
}
