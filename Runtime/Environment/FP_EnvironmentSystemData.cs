namespace FuzzPhyte.Utility.Animation.Environment
{
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using System.Collections;
    using UnityEngine;
    using System;

    [CreateAssetMenu(fileName ="FP_EnvironmentSystemData", menuName = "FuzzPhyte/Utility/Rendering/SystemData", order =21)]
    public class FP_EnvironmentSystemData:FP_Data
    {
        [Header("Managed Rig Names")]
        public string RigRootName = "FP_EnvironmentRig";
        public string VolumeRootName = "FP_EnvironmentVolume";

        [Header("Blend")]
        [Min(0f)]
        public float DefaultBlendSeconds = 0.75f;

        [Header("Quality / Updates")]
        public bool UpdateDynamicGI = true;
        public bool ForceReflectionProbeUpdate = false; // if you have probes you want to refresh

        [Header("Skybox Materials")]
        public Material SkyboxSingleCubemapMaterial; // e.g., Unity “Skybox/Cubemap”
        public string SingleCubemapProperty = "_Tex"; // Skybox/Cubemap uses _Tex

        public Material SkyboxBlendMaterial; // your blend shader material
        public string BlendCubeAProperty = "_CubeA";
        public string BlendCubeBProperty = "_CubeB";
        public string BlendAmountProperty = "_Blend";

        [Header("Skybox Blend Updates")]
        public bool UpdateGIAtBlendEndOnly = true;
        [Min(0f)] public float UpdateGIIntervalSeconds = 0.25f; // if you want interval updates
    }
}
