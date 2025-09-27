namespace FuzzPhyte.Utility.Animation
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Animations;
    using System.Collections;
    using System;
    public class FPAnimationInjector : MonoBehaviour, IAnimInjection
    {
        [Header("Defaults")]
        [SerializeField] protected float defaultFadeIn = 0.1f;
        [SerializeField] protected float defaultFadeOut = 0.1f;
        [SerializeField] protected float defaultLayerWeight = 1f;
        [SerializeField] protected AvatarMask defaultMask;
        [SerializeField] protected bool defaultAdditive = false;
        [SerializeField] protected Animator animator;
        
        protected PlayableGraph _graph;
        protected AnimationLayerMixerPlayable _layerMixer;
        protected AnimatorControllerPlayable _controllerPlayable;
        protected Coroutine activeRoutine;

        protected const int BASE_INPUT = 0;
        protected const int INJECT_INPUT = 1;
        protected string INJECTOR_NAME_SUFFIX = "_FPAnimationInjector";
        protected string INJECTOR_OUTPUT_SUFFIX = "_FPAnimationOutput";

        protected bool _initialized;
        protected bool isInjecting;
        protected bool abortRequested;
        protected float abortFadeOut = -1f;
        //protected AnimationClipPlayable? _activeClipPlayable;

        public Action<AnimationClip> OnInjectionStarted;
        public Action<AnimationClip> OnInjectionCompleted;

        #region Unity Methods
        protected virtual void Awake()
        {
            if (animator == null)
            {
                Debug.LogWarning($"Missing an animator, checking if one exists on object");
                if(this.transform.GetComponent<Animator>() != null)
                {
                    animator = this.transform.GetComponent<Animator>();
                }
                else
                {
                    Debug.LogError($"For sure missing an animator, this will fail");
                }
            }
        }
        protected virtual void OnDisable()
        {
            if (_graph.IsValid())
            {
                _graph.Stop();
                _graph.Destroy();
            }
            _initialized = false;
            isInjecting = false;
        }
        #endregion
        #region public accessors
        #region IAnimInjection Interface
        /// <summary>
        /// Inject an animation clip into the current animator
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="fadeIn"></param>
        /// <param name="fadeOut"></param>
        /// <param name="targetWeight"></param>
        /// <param name="mask"></param>
        /// <param name="additive"></param>
        /// <param name="speed"></param>
        /// <param name="backToOriginal">If you want to return to whatever animation clip we injected into</param>
        public void PlayClip(AnimationClip clip,
                     float fadeIn = -1f,
                     float fadeOut = -1f,
                     float targetWeight = -1f,
                     AvatarMask mask = null,
                     bool additive = false,
                     float speed = 1f,
                     bool backToOriginal=true)
        {
            InitializeGraphIfNeeded();
            if (!clip) { Debug.LogWarning("Null clip"); return; }
            // Start a handoff coroutine
            StartCoroutine(PlayClipHandoffRoutine(clip, fadeIn, fadeOut, targetWeight, mask, additive, speed,backToOriginal));
        }

        /// <summary>
        /// Requests a smooth stop of the current injection. 
        /// Does not kill coroutines or run its own fade; the active routine will honor this.
        /// <param name="fadeOut">time to fade out default 0.1f</param>
        /// </summary>
        public void StopActiveInjection(float fadeOut = 0.1f)
        {
            if (!isInjecting || !_graph.IsValid()) return;
            abortFadeOut = Mathf.Max(0f, fadeOut);
            abortRequested = true;
        }
        #endregion
        #endregion
        #region IEnumerators for State/Flow
        protected IEnumerator PlayClipHandoffRoutine(AnimationClip clip,
                                    float fadeIn, float fadeOut, float targetWeight,
                                    AvatarMask mask, bool additive, float speed,bool BackToOriginal=true)
        {
            if (clip == null || !_graph.IsValid())
            {
                Debug.LogError($"Missing clip information and/or grid is not valid");
                yield break;
            }
            // If something is currently injecting, request an early, smooth stop and wait
            if (isInjecting && activeRoutine != null)
            {
                // Ask the active routine to exit gracefully with a quick fade
                abortFadeOut = 0.05f;       // "speed/end smoothly"
                abortRequested = true;

                // Wait until the active routine finishes cleanup
                while (isInjecting) yield return null;
                abortRequested = false;
                abortFadeOut = -1f;
            }

            // Now we own the injector; run the normal clip routine
            activeRoutine = StartCoroutine(PlayClipRoutine(clip, fadeIn, fadeOut, targetWeight, mask, additive, speed,BackToOriginal));
        }
        protected IEnumerator PlayClipRoutine(AnimationClip clip, float fadeIn, float fadeOut, float targetWeight,AvatarMask mask, bool additive, float speed, bool returnOriginal)
        {
            float fin = (fadeIn >= 0f) ? fadeIn : defaultFadeIn;
            float fout = (fadeOut >= 0f) ? fadeOut : defaultFadeOut;
            float tw = (targetWeight >= 0f) ? targetWeight : defaultLayerWeight;
            mask = mask ? mask : defaultMask;
            additive = additive || defaultAdditive;

            // Create and connect the new clip playable.
            var clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            clipPlayable.SetApplyFootIK(true);
            clipPlayable.SetApplyPlayableIK(true);
            clipPlayable.SetSpeed(speed);
            clipPlayable.SetTime(0);

            // NEW: configure duration/loop behavior depending on returnOriginal
            if (returnOriginal)
            {
                // play once, then hand control back to base
                float duration = clip.length / Mathf.Max(0.0001f, Mathf.Abs(speed));
                clipPlayable.SetDuration(duration);
                //clipPlayable.SetLoopTime(false); // play through once
            }
            else
            {
                // stay on this injected clip indefinitely until aborted
                clipPlayable.SetDuration(double.PositiveInfinity);
                //clipPlayable.SetLoopTime(true); // loop the injected clip
            }

            if (_layerMixer.GetInput(INJECT_INPUT).IsValid())
            {
                _layerMixer.DisconnectInput(INJECT_INPUT);
            }
               
            _layerMixer.ConnectInput(INJECT_INPUT, clipPlayable, 0);
            _layerMixer.SetInputWeight(INJECT_INPUT, 0f);

            if (mask) _layerMixer.SetLayerMaskFromAvatarMask((uint)INJECT_INPUT, mask);
            _layerMixer.SetLayerAdditive((uint)INJECT_INPUT, additive);

            isInjecting = true;

            // Fade in
            if (fin > 0f)
                yield return FadeLayerWeight(INJECT_INPUT, 0f, tw, fin);
            else
                _layerMixer.SetInputWeight(INJECT_INPUT, tw);

            // --- Play phase ---
            if (returnOriginal)
            {
                // default behavior: wait for the clip to finish (or an abort)
                while (_graph.IsValid() && clipPlayable.IsValid() && !clipPlayable.IsDone())
                {
                    if (abortRequested) break;
                    yield return null;
                }
            }
            else
            {
                bool clipLoops = clip.isLooping;
                if (clipLoops)
                {
                    // Natural loop – just idle until someone calls StopActiveInjection()
                    while (_graph.IsValid() && clipPlayable.IsValid() && !abortRequested)
                        yield return null;
                }
                else
                {
                    // Force loop by wrapping time ourselves.
                    // Freeze the playable's internal clock and drive time each frame.
                    clipPlayable.SetSpeed(0); // important: we control time manually now

                    double len = Mathf.Max(clip.length, 0.0001f);
                    // Start from current time so we respect fade-in start
                    double t = clipPlayable.GetTime();
                    // Respect requested playback speed direction/magnitude
                    double spd = Mathf.Approximately(speed, 0f) ? 1.0 : speed;

                    while (_graph.IsValid() && clipPlayable.IsValid() && !abortRequested)
                    {
                        t += Time.deltaTime * spd;

                        // wrap t into [0, len)
                        t = t % len;
                        if (t < 0) t += len;

                        clipPlayable.SetTime(t);
                        // no need to Evaluate() manually; the graph updates each frame in GameTime mode
                        yield return null;
                    }

                    // Restore the playable speed so your fade-out uses normal evaluation
                    clipPlayable.SetSpeed(1);
                }
            }
            Debug.LogWarning($"Injector ending...");
            // Choose fade-out duration (abort may force a faster one)
            float foutActual = (abortRequested && abortFadeOut >= 0f) ? abortFadeOut : fout;

            // Fade out
            float current = _layerMixer.GetInputWeight(INJECT_INPUT);
            if (foutActual > 0f)
                yield return FadeLayerWeight(INJECT_INPUT, current, 0f, foutActual);
            else
                _layerMixer.SetInputWeight(INJECT_INPUT, 0f);

            // Cleanup this playable (self-contained)
            if (_layerMixer.GetInput(INJECT_INPUT).IsValid())
                _layerMixer.DisconnectInput(INJECT_INPUT);

            if (clipPlayable.IsValid())
                clipPlayable.Destroy();

            // Reset state/flags
            _layerMixer.SetInputWeight(INJECT_INPUT, 0f);
            isInjecting = false;
            abortRequested = false;
            activeRoutine = null;
        }
        /// <summary>
        /// Core Clip Routine
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="fadeIn"></param>
        /// <param name="fadeOut"></param>
        /// <param name="targetWeight"></param>
        /// <param name="mask"></param>
        /// <param name="additive"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        [Obsolete("Use PlayClipRoutine")]
        protected IEnumerator PlayClipRoutineOld(AnimationClip clip,float fadeIn, float fadeOut, float targetWeight, AvatarMask mask, bool additive, float speed, bool returnOriginal)
        {
            float fin = (fadeIn >= 0f) ? fadeIn : defaultFadeIn;
            float fout = (fadeOut >= 0f) ? fadeOut : defaultFadeOut;
            float tw = (targetWeight >= 0f) ? targetWeight : defaultLayerWeight;
            mask = mask ? mask : defaultMask;
            additive = additive || defaultAdditive;

            // Create and connect the new clip playable.
            var clipPlayable = AnimationClipPlayable.Create(_graph, clip);
            clipPlayable.SetApplyFootIK(true);
            clipPlayable.SetApplyPlayableIK(true);
            clipPlayable.SetSpeed(speed);
            clipPlayable.SetTime(0);
            float duration = clip.length / Mathf.Max(0.0001f, Mathf.Abs(speed));
            clipPlayable.SetDuration(duration);

            if (_layerMixer.GetInput(INJECT_INPUT).IsValid())
                _layerMixer.DisconnectInput(INJECT_INPUT);

            _layerMixer.ConnectInput(INJECT_INPUT, clipPlayable, 0);
            _layerMixer.SetInputWeight(INJECT_INPUT, 0f);

            if (mask) _layerMixer.SetLayerMaskFromAvatarMask((uint)INJECT_INPUT, mask);
            _layerMixer.SetLayerAdditive((uint)INJECT_INPUT, additive);


            isInjecting = true;

            // Fade in
            if (fin > 0f)
            {
                yield return FadeLayerWeight(INJECT_INPUT, 0f, tw, fin);
            }
            else
            {
                _layerMixer.SetInputWeight(INJECT_INPUT, tw);
            }

            // Play to completion OR until abort is requested
            while (_graph.IsValid() && clipPlayable.IsValid() && !clipPlayable.IsDone())
            {
                if (abortRequested) break;
                yield return null;
            }
            // CHATGPT this is where we would determine if we are going to return to the original or loop the latest one? CHAT GPT HELP!
            
            // Choose fade-out duration (abort may force a faster one)
            float foutActual = (abortRequested && abortFadeOut >= 0f) ? abortFadeOut : fout;

            // Fade out
            float current = _layerMixer.GetInputWeight(INJECT_INPUT);
            if (foutActual > 0f)
            {
                yield return FadeLayerWeight(INJECT_INPUT, current, 0f, foutActual);
            }
            else
            {
                _layerMixer.SetInputWeight(INJECT_INPUT, 0f);
            }
            // Cleanup this playable (self-contained)
            if (_layerMixer.GetInput(INJECT_INPUT).IsValid())
            {
                _layerMixer.DisconnectInput(INJECT_INPUT);
            }
            if (clipPlayable.IsValid())
            {
                clipPlayable.Destroy();
            }
              
            // Reset state/flags
            _layerMixer.SetInputWeight(INJECT_INPUT, 0f);
            isInjecting = false;
            abortRequested = false;
            activeRoutine = null;
        }

        
        protected IEnumerator WaitThenFadeOut(AnimationClipPlayable playable, float fadeOut, System.Action onComplete)
        {
            // let the clip play to completion
            while (_graph.IsValid() && playable.IsValid() && !playable.IsDone())
                yield return null;

            // then fade down the layer
            float current = _layerMixer.GetInputWeight(INJECT_INPUT);
            yield return FadeLayerWeight(INJECT_INPUT, current, 0f, fadeOut, onComplete);
        }
        protected IEnumerator FadeLayerWeight(int layer, float from, float to, float duration, System.Action onComplete = null)
        {
            if (duration <= 0f)
            {
                _layerMixer.SetInputWeight(layer, to);
                onComplete?.Invoke();
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                _layerMixer.SetInputWeight(layer, Mathf.Lerp(from, to, a));
                yield return null;
            }
            _layerMixer.SetInputWeight(layer, to);
            onComplete?.Invoke();
        }
        #endregion
        #region Helper Functions
        /// <summary>
        /// Sets up the required parameters we need to inject/fade-in/out
        /// </summary>
        protected void InitializeGraphIfNeeded()
        {
            if (_initialized) return;

            // Build graph
            _graph = PlayableGraph.Create($"{name + INJECTOR_NAME_SUFFIX}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(_graph, $"{name + INJECTOR_OUTPUT_SUFFIX}", animator);

            // Base controller playable (whatever your Animator is already using)
            var rac = animator.runtimeAnimatorController;
            if (rac == null)
            {
                Debug.LogWarning("Animator has no RuntimeAnimatorController; FPAnimationInjector requires one for base input.");
            }
            _controllerPlayable = AnimatorControllerPlayable.Create(_graph, rac);

            // Layer mixer with 2 inputs: base + injectable clip
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);
            _layerMixer.ConnectInput(BASE_INPUT, _controllerPlayable, 0);
            _layerMixer.SetInputWeight(BASE_INPUT, 1f);

            // Prepare inject input empty (disconnected until needed)
            _layerMixer.SetInputWeight(INJECT_INPUT, 0f);

            // Route to Animator
            output.SetSourcePlayable(_layerMixer);

            _graph.Play();
            _initialized = true;
        }

        /// <summary>
        /// Cleanup script
        /// </summary>
        protected void CleanupActive()
        {
            // If the graph/mixer is gone (e.g., OnDisable), just reset flags safely.
            if (!_graph.IsValid() || !_layerMixer.IsValid())
            {
                isInjecting = false;
                abortRequested = false;
                activeRoutine = null;
                return;
            }

            // Ensure the injected layer has zero weight before disconnecting
            if (_layerMixer.GetInputCount() > INJECT_INPUT)
            {
                // Set weight to 0 if not already
                if (_layerMixer.GetInputWeight(INJECT_INPUT) != 0f)
                    _layerMixer.SetInputWeight(INJECT_INPUT, 0f);

                // Disconnect the injected input if it’s connected/valid
                var injected = _layerMixer.GetInput(INJECT_INPUT);
                if (injected.IsValid())
                    _layerMixer.DisconnectInput(INJECT_INPUT);
            }
            // Reset state/flags
           
            isInjecting = false;
            abortRequested = false;
            activeRoutine = null;
        }
        #endregion

    }
}
