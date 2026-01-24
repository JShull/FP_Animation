namespace FuzzPhyte.Utility.Animation.Environment
{
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using UnityEngine;
    using System;

    [CreateAssetMenu(fileName = "FP_EnvironmentProfile", menuName = "FuzzPhyte/Utility/Rendering/Environment Profile", order = 20)]
    public class FP_EnvironmentProfile:FP_Data
    {
        [Header("Skybox")]
        public bool UseBlendCubemaps = true;
        public Cubemap SkyCubemap;
        public Material SkyboxMaterial;

        [Header("Ambient")]
        public AmbientMode AmbientMode = AmbientMode.Skybox;
        public Color AmbientSkyColor = Color.gray;
        public Color AmbientEquatorColor = Color.gray;
        public Color AmbientGroundColor = Color.gray;
        [Range(0f, 8f)]
        public float AmbientIntensity = 1f;

        [Header("Reflections")]
        public DefaultReflectionMode DefaultReflectionMode = DefaultReflectionMode.Skybox;
        public Cubemap CustomReflectionCubemap;
        [Range(0f, 1f)]
        public float ReflectionIntensity = 1f;
        public int ReflectionBounces = 1;

        [Header("Fog (Optional)")]
        public bool UseFog = false;
        public FogMode FogMode = FogMode.ExponentialSquared;
        public Color FogColor = Color.gray;
        public float FogDensity = 0.01f;
        public float FogStartDistance = 0f;
        public float FogEndDistance = 300f;

        [Header("Key Light (Directional)")]
        public FP_LightDefinition KeyLight = FP_LightDefinition.CreateDirectionalDefault();

        [Header("Additional Lights")]
        public List<FP_LightDefinition> AdditionalLights = new List<FP_LightDefinition>();

        [Header("URP Volume (Optional)")]
        public VolumeProfile VolumeProfile;
        [Range(0f, 1f)]
        public float VolumeWeight = 1f;
    }

    [Serializable]
    public struct FP_LightDefinition
    {
        public string Name;
        public LightType Type;
        public Color Color;
        [Min(0f)]
        public float Intensity;
        public LightShadows Shadows;

        // Transform-ish
        public Vector3 Position;
        public Vector3 EulerAngles; // useful for directional/spot
        public float Range;         // point/spot
        [Range(1f, 179f)]
        public float SpotAngle;     // spot

        // Optional “active”
        public bool Enabled;

        public static FP_LightDefinition CreateDirectionalDefault()
        {
            return new FP_LightDefinition
            {
                Name = "FP_KeyLight",
                Type = LightType.Directional,
                Color = Color.white,
                Intensity = 1f,
                Shadows = LightShadows.Soft,
                Position = Vector3.zero,
                EulerAngles = new Vector3(50f, -30f, 0f),
                Range = 10f,
                SpotAngle = 30f,
                Enabled = true
            };
        }
    }
}
