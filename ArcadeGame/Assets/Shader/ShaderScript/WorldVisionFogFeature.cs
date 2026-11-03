using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class WorldVisionFogFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader fogShader;

    [SerializeField] private Color fogColor = Color.black;

    [SerializeField, Range(0f, 1f)]
    private float outsideDarkness = 0.7f;

    [SerializeField, Range(0f, 1f)]
    private float insideDarkness = 0.05f;

    private Material _material;
    private FogPass _pass;

    public override void Create()
    {
        ReleaseResources();

        if (fogShader == null || !fogShader.isSupported)
            return;

        _material = CoreUtils.CreateEngineMaterial(fogShader);
        _pass = new FogPass(_material);
    }

    private bool ShouldRender(in RenderingData data)
    {
        WorldVisionMaskCamera source = WorldVisionMaskCamera.Active;

        return _material != null
            && _pass != null
            && source != null
            && source.MaskTexture != null
            && source.LastRenderedFrame == Time.frameCount
            && data.cameraData.cameraType == CameraType.Game
            && data.cameraData.renderType == CameraRenderType.Base
            && data.cameraData.camera == source.MainCamera;
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

        WorldVisionMaskCamera source = WorldVisionMaskCamera.Active;

        _pass.Setup(
            renderer.cameraColorTargetHandle,
            source.MaskTexture,
            source.WorldToMaskUV,
            fogColor,
            outsideDarkness,
            insideDarkness);
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

    private sealed class FogPass : ScriptableRenderPass
    {
        private static readonly int MaskId =
            Shader.PropertyToID("_VisionMask");

        private static readonly int MappingId =
            Shader.PropertyToID("_WorldToMaskUV");

        private static readonly int ColorId =
            Shader.PropertyToID("_FogColor");

        private static readonly int OutsideId =
            Shader.PropertyToID("_OutsideDarkness");

        private static readonly int InsideId =
            Shader.PropertyToID("_InsideDarkness");

        private readonly Material _material;

        private readonly ProfilingSampler _sampler =
            new ProfilingSampler("World Vision Fog");

        private RTHandle _source;
        private RTHandle _temporary;

        public FogPass(Material material)
        {
            _material = material;

            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(
            RTHandle source,
            RenderTexture mask,
            Vector4 mapping,
            Color color,
            float outside,
            float inside)
        {
            _source = source;

            _material.SetTexture(MaskId, mask);
            _material.SetVector(MappingId, mapping);
            _material.SetColor(ColorId, color);
            _material.SetFloat(OutsideId, Mathf.Clamp01(outside));
            _material.SetFloat(InsideId, Mathf.Clamp01(inside));
        }

        public override void OnCameraSetup(
            CommandBuffer cmd,
            ref RenderingData renderingData)
        {
            if (_source == null)
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
                name: "_WorldVisionFogTemporary");

            ConfigureTarget(_source);
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            if (_material == null ||
                _source == null ||
                _temporary == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            try
            {
                using (new ProfilingScope(cmd, _sampler))
                {
                    Blitter.BlitCameraTexture(
                        cmd,
                        _source,
                        _temporary,
                        _material,
                        0);

                    Blitter.BlitCameraTexture(
                        cmd,
                        _temporary,
                        _source);
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