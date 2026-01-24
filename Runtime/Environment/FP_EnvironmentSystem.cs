namespace FuzzPhyte.Utility.Animation.Environment
{
    using FuzzPhyte.Utility.FPSystem;
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using System.Collections;
    using UnityEngine;
    using System;

    public class FP_EnvironmentSystem : FPSystemBase<FP_EnvironmentSystemData>
    {
        public event Action<FP_EnvironmentProfile> OnProfileApplied;

        private GameObject _rigRoot;
        private Light _keyLight;
        private readonly List<Light> _extraLights = new List<Light>();

        private GameObject _volumeRoot;
        private Volume _volume;

        private Coroutine _blendRoutine;
        private readonly FP_SkyboxBlender _skyboxBlender = new FP_SkyboxBlender();
        private FP_EnvironmentProfile _currentProfile;
        private float _nextGIUpdateTime;
        // cached
        private Material _singleSkyboxRuntimeMat;
        private int _singleCubemapTexId;

        public override void Awake()
        {
            base.Awake();
            EndOfFrame ??= new WaitForEndOfFrame();
        }
        public override void OnDestroy()
        {
            if (_singleSkyboxRuntimeMat != null) Destroy(_singleSkyboxRuntimeMat);
            if (_skyboxBlender != null && _skyboxBlender.RuntimeMaterial != null) Destroy(_skyboxBlender.RuntimeMaterial);
        }

        public override void Initialize(bool runAfterLateUpdateLoop, FP_EnvironmentSystemData data = null)
        {
            base.Initialize(runAfterLateUpdateLoop, data);
            Debug.LogWarning($"Initialize Skybox FP Environment System");
            EnsureRig();
            EnsureVolume();
        }

        public void ApplyProfile(FP_EnvironmentProfile profile)
        {
            if (profile == null) return;

            EnsureRig();
            EnsureVolume();

            ApplySkybox(profile);
            ApplyNonSkyRenderSettings(profile);

            ApplyLights(profile);
            ApplyVolume(profile);

            _currentProfile = profile;

            if (systemData != null && systemData.UpdateDynamicGI)
                DynamicGI.UpdateEnvironment();

            OnProfileApplied?.Invoke(profile);
        }

        public void BlendToProfile(FP_EnvironmentProfile target, float seconds = -1f)
        {
            if (target == null) return;

            float dur = seconds >= 0f ? seconds : (systemData != null ? systemData.DefaultBlendSeconds : 0.75f);

            if (_blendRoutine != null)
            {
                StopCoroutine(_blendRoutine);
                _blendRoutine = null;
            }

            _blendRoutine = StartCoroutine(BlendRoutine(target, dur));
        }

        private IEnumerator BlendRoutine(FP_EnvironmentProfile target, float seconds)
        {
            EnsureRig();
            EnsureVolume();

            if (target == null)
            {
                _blendRoutine = null;
                yield break;
            }

            // Capture current settings
            var startAmbientIntensity = RenderSettings.ambientIntensity;
            var startReflectionIntensity = RenderSettings.reflectionIntensity;

            var startKeyColor = _keyLight != null ? _keyLight.color : Color.white;
            var startKeyIntensity = _keyLight != null ? _keyLight.intensity : 1f;
            var startKeyEuler = _keyLight != null ? _keyLight.transform.eulerAngles : Vector3.zero;

            float startVolWeight = _volume != null ? _volume.weight : 0f;

            // --- Skybox setup ---
            bool targetHasCube = target.UseBlendCubemaps && target.SkyCubemap != null;
            bool systemHasBlendMat = systemData != null && systemData.SkyboxBlendMaterial != null;

            // "from" cubemap only exists if we have a current profile with a cubemap
            Cubemap fromCube = null;
            if (_currentProfile != null && _currentProfile.UseBlendCubemaps && _currentProfile.SkyCubemap != null)
                fromCube = _currentProfile.SkyCubemap;

            bool canBlendSky = systemHasBlendMat && targetHasCube && fromCube != null;

            // If we can't do a real cubemap->cubemap blend, decide how to handle skybox:
            // - If target has a single skybox material, we will snap at end.
            // - If target has a cubemap but no fromCube, we can just snap to target's cubemap skybox at start.
            bool didAssignBlendMaterial = false;

            if (canBlendSky)
            {
                _skyboxBlender.Ensure(systemData.SkyboxBlendMaterial);
                _skyboxBlender.BeginBlend(fromCube, target.SkyCubemap);
                didAssignBlendMaterial = _skyboxBlender.IsReady;
            }
            else
            {
                // First-time blend (no current cube), or missing blend material:
                // Prefer snapping to target skybox *now* if target provides cubemap.
                // Otherwise we do nothing here and let ApplyProfile(target) finalize at end.
                if (targetHasCube)
                {
                    // If you have a "single cubemap skybox material" path, you can call that here.
                    // Otherwise you can just rely on ApplyProfile(target) at the end.
                    // I recommend: apply skybox immediately for visual continuity.
                    ApplySkybox(target);
                }
            }

            _nextGIUpdateTime = Time.time + (systemData != null ? systemData.UpdateGIIntervalSeconds : 0.25f);

            float t = 0f;
            float invSeconds = seconds <= 0f ? 0f : (1f / seconds);

            while (t < 1f)
            {
                t = seconds <= 0f ? 1f : Mathf.Clamp01(t + Time.deltaTime * invSeconds);

                // Skybox blend parameter + optional GI interval updates
                if (canBlendSky && didAssignBlendMaterial)
                {
                    _skyboxBlender.SetBlend01(t);

                    if (systemData != null && systemData.UpdateDynamicGI && !systemData.UpdateGIAtBlendEndOnly)
                    {
                        if (Time.time >= _nextGIUpdateTime)
                        {
                            DynamicGI.UpdateEnvironment();
                            _nextGIUpdateTime = Time.time + systemData.UpdateGIIntervalSeconds;
                        }
                    }
                }

                // Ambient / reflection (minimal blend)
                RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, target.AmbientIntensity, t);
                RenderSettings.reflectionIntensity = Mathf.Lerp(startReflectionIntensity, target.ReflectionIntensity, t);

                // Key light blend
                if (_keyLight != null)
                {
                    _keyLight.color = Color.Lerp(startKeyColor, target.KeyLight.Color, t);
                    _keyLight.intensity = Mathf.Lerp(startKeyIntensity, target.KeyLight.Intensity, t);
                    _keyLight.transform.eulerAngles = Vector3.Lerp(startKeyEuler, target.KeyLight.EulerAngles, t);
                }

                // Volume blend
                if (_volume != null)
                {
                    float targetW = target.VolumeProfile != null ? target.VolumeWeight : 0f;
                    _volume.weight = Mathf.Lerp(startVolWeight, targetW, t);
                }

                yield return null;
            }

            // If we were blending skybox, lock blend to 1 and keep the blend material assigned.
            // Then ApplyProfile can still update ambient/fog/reflections/lights/volume, but we skip its skybox assignment.
            if (canBlendSky && didAssignBlendMaterial)
            {
                _skyboxBlender.SetBlend01(1f);
                RenderSettings.skybox = _skyboxBlender.RuntimeMaterial;
            }
            else
            {
                ApplySkybox(target);
            }
            // Always apply the rest of the settings + lights/volume
            ApplyNonSkyRenderSettings(target);
            ApplyLights(target);
            ApplyVolume(target);

            _currentProfile = target;
            OnProfileApplied?.Invoke(target);

            // GI update strategy
            if (systemData != null && systemData.UpdateDynamicGI)
            {
                if (!canBlendSky || systemData.UpdateGIAtBlendEndOnly)
                    DynamicGI.UpdateEnvironment();
            }

            _blendRoutine = null;
        }
        

        private void ApplySkybox(FP_EnvironmentProfile p)
        {
            if (p.UseBlendCubemaps && p.SkyCubemap != null && systemData != null && systemData.SkyboxSingleCubemapMaterial != null)
            {
                EnsureSingleSkyboxMaterial();
                if (_singleSkyboxRuntimeMat != null)
                {
                    _singleSkyboxRuntimeMat.SetTexture(_singleCubemapTexId, p.SkyCubemap);
                    RenderSettings.skybox = _singleSkyboxRuntimeMat;
                }
            }
            else
            {
                RenderSettings.skybox = p.SkyboxMaterial;
            }
        }

        private void ApplyNonSkyRenderSettings(FP_EnvironmentProfile p)
        {
            // Ambient
            RenderSettings.ambientMode = p.AmbientMode;
            RenderSettings.ambientSkyColor = p.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = p.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = p.AmbientGroundColor;
            RenderSettings.ambientIntensity = p.AmbientIntensity;

            // Reflections
            RenderSettings.defaultReflectionMode = p.DefaultReflectionMode;
            RenderSettings.customReflectionTexture = p.CustomReflectionCubemap;
            RenderSettings.reflectionIntensity = p.ReflectionIntensity;
            RenderSettings.reflectionBounces = p.ReflectionBounces;

            // Fog
            RenderSettings.fog = p.UseFog;
            if (p.UseFog)
            {
                RenderSettings.fogMode = p.FogMode;
                RenderSettings.fogColor = p.FogColor;
                RenderSettings.fogDensity = p.FogDensity;
                RenderSettings.fogStartDistance = p.FogStartDistance;
                RenderSettings.fogEndDistance = p.FogEndDistance;
            }
        }

        private void EnsureRig()
        {
            if (systemData == null)
            {
                // systemData is optional in your base; still operate
            }

            if (_rigRoot == null)
            {
                string rootName = systemData != null ? systemData.RigRootName : "FP_EnvironmentRig";
                _rigRoot = GameObject.Find(rootName);
                if (_rigRoot == null)
                {
                    _rigRoot = new GameObject(rootName);
                    DontDestroyOnLoad(_rigRoot);
                }
            }

            if (_keyLight == null)
            {
                _keyLight = FindOrCreateLight(_rigRoot.transform, "FP_KeyLight", LightType.Directional);
            }
        }

        private void EnsureVolume()
        {
            string volRootName = systemData != null ? systemData.VolumeRootName : "FP_EnvironmentVolume";

            if (_volumeRoot == null)
            {
                _volumeRoot = GameObject.Find(volRootName);
                if (_volumeRoot == null)
                {
                    _volumeRoot = new GameObject(volRootName);
                    _volumeRoot.transform.SetParent(_rigRoot != null ? _rigRoot.transform : null);
                    DontDestroyOnLoad(_volumeRoot);
                }
            }

            if (_volume == null)
            {
                _volume = _volumeRoot.GetComponent<Volume>();
                if (_volume == null) _volume = _volumeRoot.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 100; // high-ish, but you can tune this
            }
        }
        
       
        private void EnsureSingleSkyboxMaterial()
        {
            if (systemData == null || systemData.SkyboxSingleCubemapMaterial == null)
                return;

            if (_singleSkyboxRuntimeMat != null)
                return;

            _singleCubemapTexId = Shader.PropertyToID(systemData.SingleCubemapProperty);

            _singleSkyboxRuntimeMat = Instantiate(systemData.SkyboxSingleCubemapMaterial);
            _singleSkyboxRuntimeMat.name = $"{systemData.SkyboxSingleCubemapMaterial.name}_RuntimeSingle";
        }
        private void ApplyLights(FP_EnvironmentProfile p)
        {
            // Key light (always directional)
            if (_keyLight != null)
            {
                ApplyLightDefinition(_keyLight, p.KeyLight);
                _keyLight.type = LightType.Directional;
            }

            // Ensure extra lights count
            int needed = p.AdditionalLights != null ? p.AdditionalLights.Count : 0;

            // Create if not enough
            while (_extraLights.Count < needed)
            {
                var idx = _extraLights.Count;
                var l = FindOrCreateLight(_rigRoot.transform, $"FP_ExtraLight_{idx:00}", LightType.Point);
                _extraLights.Add(l);
            }

            // Apply / enable needed
            for (int i = 0; i < _extraLights.Count; i++)
            {
                bool active = i < needed;
                _extraLights[i].gameObject.SetActive(active);

                if (!active) continue;

                var def = p.AdditionalLights[i];
                _extraLights[i].name = string.IsNullOrWhiteSpace(def.Name) ? _extraLights[i].name : def.Name;
                _extraLights[i].type = def.Type;
                ApplyLightDefinition(_extraLights[i], def);
            }
        }

        private void ApplyVolume(FP_EnvironmentProfile p)
        {
            if (_volume == null) return;

            if (p.VolumeProfile == null)
            {
                _volume.profile = null;
                _volume.weight = 0f;
                return;
            }

            _volume.profile = p.VolumeProfile;
            _volume.weight = p.VolumeWeight;
        }

        private static Light FindOrCreateLight(Transform parent, string name, LightType type)
        {
            Transform t = parent != null ? parent.Find(name) : null;
            GameObject go;

            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(name);
                if (parent != null) go.transform.SetParent(parent);
            }

            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = type;
            return light;
        }

        private static void ApplyLightDefinition(Light l, FP_LightDefinition def)
        {
            l.enabled = def.Enabled;
            l.color = def.Color;
            l.intensity = def.Intensity;
            l.shadows = def.Shadows;

            l.transform.position = def.Position;
            l.transform.eulerAngles = def.EulerAngles;

            l.range = def.Range;
            l.spotAngle = def.SpotAngle;
        }
    }
}
