using UnityEngine;

namespace FuzzPhyte.Utility.Animation
{
    public interface IAnimInjection
    {
        public void PlayClip(AnimationClip clip,float fadeIn = -1f,float fadeOut = -1f,float targetWeight = -1f,AvatarMask mask = null,bool additive = false,float speed = 1f,bool backToOriginal=true);

        public void StopActiveInjection(float fadeOut = 0.1f);
    
    }
}
