using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class WorldVisionMaskCamera : MonoBehaviour
{
    public static WorldVisionMaskCamera Active { get; private set; }

    private const float MinimumSize = 1f;
    private const float MinimumHeight = 1f;
    private const float NearClip = 0.1f;
    private const float CameraOrderOffset = 1f;

    private static readonly Quaternion TopDownRotation =
        Quaternion.Euler(90f, 0f, 0f);

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform followTarget;

    [Header("World Coverage")]
    [Tooltip("마스크가 포함하는 월드 영역의 세로 반경입니다.")]
    [SerializeField, Min(MinimumSize)]
    private float halfHeight = 28f;

    [SerializeField] private float groundHeight = 0f;

    [SerializeField, Min(MinimumHeight)]
    private float cameraHeight = 50f;

    private Camera _maskCamera;
    private Transform _cachedTransform;

    public Camera MainCamera => mainCamera;

    public RenderTexture MaskTexture =>
        _maskCamera != null ? _maskCamera.targetTexture : null;

    // xy: 월드 XZ를 UV로 변환하는 배율
    // zw: 월드 XZ를 UV로 변환하는 오프셋
    public Vector4 WorldToMaskUV { get; private set; }

    public int LastRenderedFrame { get; private set; } = -1;

    private void Awake()
    {
        TryGetComponent(out _maskCamera);
        _cachedTransform = transform;

        if (_maskCamera == null ||
            mainCamera == null ||
            mainCamera == _maskCamera ||
            followTarget == null ||
            _maskCamera.targetTexture == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(
                "WorldVisionMaskCamera의 Main Camera, Follow Target, " +
                "FOVCamera의 Target Texture를 확인해 주세요.",
                this);
#endif
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_maskCamera == null || mainCamera == null)
            return;

        if (Active != null && Active != this)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(
                "WorldVisionMaskCamera는 하나만 활성화해 주세요.",
                this);
#endif
            enabled = false;
            return;
        }

        Active = this;
        LastRenderedFrame = -1;

        // 메인 카메라보다 먼저 마스크를 렌더링합니다.
        _maskCamera.depth = mainCamera.depth - CameraOrderOffset;

        UpdateCameraAndMapping();

        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;

        if (Active == this)
            Active = null;

        LastRenderedFrame = -1;
    }

    private void OnBeginCamera(
        ScriptableRenderContext context,
        Camera camera)
    {
        if (camera != _maskCamera)
            return;

        UpdateCameraAndMapping();
    }

    private void OnEndCamera(
        ScriptableRenderContext context,
        Camera camera)
    {
        if (camera == _maskCamera)
            LastRenderedFrame = Time.frameCount;
    }

    private void UpdateCameraAndMapping()
    {
        if (_maskCamera == null ||
            followTarget == null ||
            _maskCamera.targetTexture == null)
        {
            return;
        }

        RenderTexture texture = _maskCamera.targetTexture;

        float aspect =
            (float)texture.width / Mathf.Max(1, texture.height);

        float size = Mathf.Max(MinimumSize, halfHeight);
        float height = Mathf.Max(MinimumHeight, cameraHeight);

        Vector3 center = followTarget.position;

        Vector3 cameraPosition = new Vector3(
            center.x,
            groundHeight + height,
            center.z);

        _cachedTransform.SetPositionAndRotation(
            cameraPosition,
            TopDownRotation);

        // 기존 카메라 동기화가 남긴 행렬도 초기화합니다.
        _maskCamera.ResetWorldToCameraMatrix();
        _maskCamera.ResetProjectionMatrix();

        _maskCamera.orthographic = true;
        _maskCamera.orthographicSize = size;
        _maskCamera.aspect = aspect;

        _maskCamera.nearClipPlane = NearClip;
        _maskCamera.farClipPlane = height + MinimumHeight;

        _maskCamera.clearFlags = CameraClearFlags.SolidColor;
        _maskCamera.backgroundColor = Color.black;
        _maskCamera.rect = new Rect(0f, 0f, 1f, 1f);

        float width = size * aspect * 2f;
        float depth = size * 2f;

        WorldToMaskUV = new Vector4(
            1f / width,
            1f / depth,
            0.5f - center.x / width,
            0.5f - center.z / depth);
    }
}