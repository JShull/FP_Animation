using UnityEngine;

namespace FuzzPhyte.Utility.Animation
{
    public struct FPAnimationData
    {
        public AnimationClip Clip;
        [Tooltip("-1")]
        public float FadeIn;
        [Tooltip("-1")]
        public float FadeOut;
        [Tooltip("-1")]
        public float TargetWeight;
        public AvatarMask Mask;
        public bool Additive;
        public float AnimationSpeed;
        [Tooltip("true")]
        public bool BackToOriginal;
    }
    public interface IAnimInjection
    {
        public void PlayClip(AnimationClip clip,float fadeIn = -1f,float fadeOut = -1f,float targetWeight = -1f,AvatarMask mask = null,bool additive = false,float speed = 1f,bool backToOriginal=true);
        public void PlayClip(FPAnimationData data);

        public void StopActiveInjection(float fadeOut = 0.1f);
    
    }
}
