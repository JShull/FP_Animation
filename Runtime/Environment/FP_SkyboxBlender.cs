namespace FuzzPhyte.Utility.Animation.Environment
{
    using UnityEngine;
    public sealed class FP_SkyboxBlender
    {
        private readonly int _cubeA = Shader.PropertyToID("_CubeA");
        private readonly int _cubeB = Shader.PropertyToID("_CubeB");
        private readonly int _blend = Shader.PropertyToID("_Blend");

        // Optional shader props (only used if the shader has them)
        private readonly int _rotationY = Shader.PropertyToID("_RotationY");
        private readonly int _tint = Shader.PropertyToID("_Tint");
        private readonly int _exposure = Shader.PropertyToID("_Exposure");

        private Material _runtimeMat;

        public bool IsReady => _runtimeMat != null;
        public Material RuntimeMaterial => _runtimeMat;

        /// <summary>
        /// Creates a runtime instance of the provided blend material asset (so we never mutate the asset).
        /// Safe to call multiple times.
        /// </summary>
        public void Ensure(Material blendMaterialAsset)
        {
            if (_runtimeMat != null) return;

            if (blendMaterialAsset == null)
            {
                Debug.LogError("FP_SkyboxBlender.Ensure failed: blendMaterialAsset was null.");
                return;
            }

            _runtimeMat = UnityEngine.Object.Instantiate(blendMaterialAsset);
            _runtimeMat.name = $"{blendMaterialAsset.name}_Runtime";

            // Validate required props exist
            if (!_runtimeMat.HasProperty(_cubeA) || !_runtimeMat.HasProperty(_cubeB) || !_runtimeMat.HasProperty(_blend))
            {
                Debug.LogError($"FP_SkyboxBlender: Assigned blend material '{blendMaterialAsset.name}' is missing one of required properties: _CubeA, _CubeB, _Blend.");
            }
        }

        /// <summary>
        /// Begins skybox blending by assigning the runtime material to RenderSettings.skybox and setting CubeA/CubeB.
        /// </summary>
        public void BeginBlend(Cubemap a, Cubemap b)
        {
            if (_runtimeMat == null)
            {
                Debug.LogError("FP_SkyboxBlender.BeginBlend called before Ensure().");
                return;
            }

            if (a == null || b == null)
            {
                Debug.LogError("FP_SkyboxBlender.BeginBlend requires both Cubemap A and Cubemap B to be non-null.");
                return;
            }

            _runtimeMat.SetTexture(_cubeA, a);
            _runtimeMat.SetTexture(_cubeB, b);
            _runtimeMat.SetFloat(_blend, 0f);

            RenderSettings.skybox = _runtimeMat;
        }

        /// <summary>
        /// Sets the blend amount (0..1). Safe clamp.
        /// </summary>
        public void SetBlend01(float t01)
        {
            if (_runtimeMat == null) return;
            _runtimeMat.SetFloat(_blend, Mathf.Clamp01(t01));
        }

        /// <summary>
        /// Optional: only effective if the shader contains _RotationY.
        /// </summary>
        public void SetRotationY(float degrees)
        {
            if (_runtimeMat == null) return;
            if (_runtimeMat.HasProperty(_rotationY))
                _runtimeMat.SetFloat(_rotationY, degrees);
        }

        /// <summary>
        /// Optional: only effective if the shader contains _Tint.
        /// </summary>
        public void SetTint(Color tint)
        {
            if (_runtimeMat == null) return;
            if (_runtimeMat.HasProperty(_tint))
                _runtimeMat.SetColor(_tint, tint);
        }

        /// <summary>
        /// Optional: only effective if the shader contains _Exposure.
        /// </summary>
        public void SetExposure(float exposure)
        {
            if (_runtimeMat == null) return;
            if (_runtimeMat.HasProperty(_exposure))
                _runtimeMat.SetFloat(_exposure, exposure);
        }
    }
}
