using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class VisionCameraSync : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("실제 게임 화면을 렌더링하는 Main Camera입니다.")]
    [SerializeField] private Camera sourceCamera;

    private Camera _visionCamera;
    private bool _hasMatrixOverride;

    private void Awake()
    {
        if (!TryGetComponent(out _visionCamera))
        {
            enabled = false;
            return;
        }

        if (sourceCamera == null || sourceCamera == _visionCamera)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(
                "VisionCameraSync에 별도의 Main Camera를 연결해 주세요.",
                this);
#endif

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_visionCamera == null || sourceCamera == null)
            return;

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        SynchronizeMatrices();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        RestoreAutomaticMatrices();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context,Camera renderingCamera)
    {
        // 다른 카메라의 렌더링에는 관여하지 않습니다.
        if (renderingCamera != _visionCamera)
            return;

        if (sourceCamera == null || !sourceCamera.isActiveAndEnabled)
            return;

        SynchronizeMatrices();
    }

    private void SynchronizeMatrices()
    {
        if (_visionCamera == null || sourceCamera == null)
            return;

        // 카메라가 바라보는 위치와 방향을 일치
        _visionCamera.worldToCameraMatrix = sourceCamera.worldToCameraMatrix;

        // 화면 비율, 줌 등을 포함한 투영을 일치
        _visionCamera.projectionMatrix = sourceCamera.projectionMatrix;
        _hasMatrixOverride = true;
    }

    private void RestoreAutomaticMatrices()
    {
        if (!_hasMatrixOverride || _visionCamera == null)
            return;

        // 컴포넌트가 꺼지면 Unity의 자동 계산으로 복귀
        _visionCamera.ResetWorldToCameraMatrix();
        _visionCamera.ResetProjectionMatrix();

        _hasMatrixOverride = false;
    }
}