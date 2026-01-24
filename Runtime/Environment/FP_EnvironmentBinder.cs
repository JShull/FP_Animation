namespace FuzzPhyte.Utility.Animation.Environment
{
    using UnityEngine;
    public class FP_EnvironmentBinder : MonoBehaviour
    {
        [Header("Profiles")]
        public FP_EnvironmentProfile OnEnableProfile;
        public FP_EnvironmentProfile OnActionProfile;

        [Header("Blend Settings")]
        public bool BlendOnEnable = true;
        public float BlendSeconds = 0.75f;

        private FP_EnvironmentSystem _system;

        private void Awake()
        {
            _system = FindFirstObjectByType<FP_EnvironmentSystem>();
        }

        private void OnEnable()
        {
            if (_system == null) return;
            if (OnEnableProfile == null) return;

            if (BlendOnEnable) _system.BlendToProfile(OnEnableProfile, BlendSeconds);
            else _system.ApplyProfile(OnEnableProfile);
        }

        [ContextMenu("Test Blending")]
        public void TriggerApplyActionProfile()
        {
            if (_system == null || OnActionProfile == null) return;
            _system.BlendToProfile(OnActionProfile, BlendSeconds);
        }
    }
}
