using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class VisionBlurRendererFeature : ScriptableRendererFeature
{
    [Header("References")]
    [SerializeField] private Shader blurShader;
    [SerializeField] private RenderTexture targetMask;

    [Header("Blur")]
    [Tooltip("마스크 픽셀 기준 흐림 강도입니다.")]
    [SerializeField, Range(0.5f, 16f)]
    private float sigma = 5f;

    private Material _material;
    private VisionBlurPass _pass;

    public override void Create()
    {
        ReleaseResources();

        if (blurShader == null || !blurShader.isSupported)
            return;

        _material = CoreUtils.CreateEngineMaterial(blurShader);
        _pass = new VisionBlurPass(_material);
    }

    private bool ShouldRender(in RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;

        return _pass != null
            && _material != null
            && targetMask != null
            && camera != null
            && camera.cameraType == CameraType.Game
            && camera.targetTexture == targetMask;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (!ShouldRender(renderingData))
            return;

        renderer.EnqueuePass(_pass);
    }

    public override void SetupRenderPasses(
        ScriptableRenderer renderer,
        in RenderingData renderingData)
    {
        if (!ShouldRender(renderingData))
            return;

        _pass.Setup(
            renderer.cameraColorTargetHandle,
            sigma);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        _pass?.Dispose();
        _pass = null;

        CoreUtils.Destroy(_material);
        _material = null;
    }

    private sealed class VisionBlurPass : ScriptableRenderPass
    {
        private const int MaxPairs = 24;
        private const float MinimumSigma = 0.5f;
        private const float MaximumSigma = 16f;
        private const float KernelExtent = 3f;

        private const int HorizontalPassIndex = 0;
        private const int VerticalPassIndex = 1;

        private static readonly int PixelSizeId =
            Shader.PropertyToID("_PixelSize");

        private static readonly int CenterWeightId =
            Shader.PropertyToID("_CenterWeight");

        private static readonly int PairCountId =
            Shader.PropertyToID("_PairCount");

        private static readonly int PairsId =
            Shader.PropertyToID("_Pairs");

        private readonly Material _material;

        private readonly Vector4[] _pairs =
            new Vector4[MaxPairs];

        private readonly ProfilingSampler _profilingSampler =
            new ProfilingSampler("Vision Gaussian Blur");

        private RTHandle _source;
        private RTHandle _temporary;

        private float _cachedSigma = -1f;

        public VisionBlurPass(Material material)
        {
            _material = material;

            renderPassEvent =
                RenderPassEvent.AfterRenderingTransparents;
        }

        public void Setup(RTHandle source, float sigma)
        {
            _source = source;

            float clampedSigma = Mathf.Clamp(
                sigma,
                MinimumSigma,
                MaximumSigma);

            if (Mathf.Approximately(_cachedSigma, clampedSigma))
                return;

            UpdateKernel(clampedSigma);
            _cachedSigma = clampedSigma;
        }

        private void UpdateKernel(float sigma)
        {
            int radius = Mathf.CeilToInt(KernelExtent * sigma);

            int pairCount = Mathf.Clamp(
                (radius + 1) / 2,
                1,
                MaxPairs);

            float inverseTwoSigmaSquared =
                1f / (2f * sigma * sigma);

            float totalWeight = 1f;

            for (int i = 0; i < pairCount; i++)
            {
                int firstOffset = i * 2 + 1;
                int secondOffset = firstOffset + 1;

                float firstWeight = Mathf.Exp(
                    -firstOffset * firstOffset
                    * inverseTwoSigmaSquared);

                float secondWeight = Mathf.Exp(
                    -secondOffset * secondOffset
                    * inverseTwoSigmaSquared);

                float combinedWeight =
                    firstWeight + secondWeight;

                float combinedOffset =
                    (firstOffset * firstWeight
                    + secondOffset * secondWeight)
                    / combinedWeight;

                _pairs[i] = new Vector4(
                    combinedOffset,
                    combinedWeight,
                    0f,
                    0f);

                totalWeight += combinedWeight * 2f;
            }

            float inverseTotalWeight = 1f / totalWeight;

            for (int i = 0; i < pairCount; i++)
            {
                Vector4 pair = _pairs[i];
                pair.y *= inverseTotalWeight;
                _pairs[i] = pair;
            }

            _material.SetFloat(
                CenterWeightId,
                inverseTotalWeight);

            _material.SetInt(PairCountId, pairCount);
            _material.SetVectorArray(PairsId, _pairs);
        }

        public override void OnCameraSetup(
            CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            if (_source == null || _material == null)
                return;

            RenderTextureDescriptor descriptor =
                renderingData.cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;

            RenderingUtils.ReAllocateIfNeeded(
                ref _temporary,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_VisionBlurTemporary");

            int sourceWidth = _source.rt != null
                ? _source.rt.width
                : descriptor.width;

            int sourceHeight = _source.rt != null
                ? _source.rt.height
                : descriptor.height;

            _material.SetVector(
                PixelSizeId,
                new Vector4(
                    1f / Mathf.Max(1, sourceWidth),
                    1f / Mathf.Max(1, sourceHeight),
                    1f / Mathf.Max(1, descriptor.width),
                    1f / Mathf.Max(1, descriptor.height)));

            ConfigureTarget(_source);
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_material == null
                || _source == null
                || _temporary == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            try
            {
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    Blitter.BlitCameraTexture(
                        cmd,
                        _source,
                        _temporary,
                        _material,
                        HorizontalPassIndex);

                    Blitter.BlitCameraTexture(
                        cmd,
                        _temporary,
                        _source,
                        _material,
                        VerticalPassIndex);
                }

                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        public void Dispose()
        {
            _temporary?.Release();
            _temporary = null;
            _source = null;
        }
    }
}