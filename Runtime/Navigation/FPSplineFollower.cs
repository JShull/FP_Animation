namespace FuzzPhyte.Utility.Animation
{
    using UnityEngine;
    using UnityEngine.Splines;
    using Unity.Mathematics;
    //Designer workflow: drop a FP_SplineFollowTrack, bind to your NPC (with FP_SplineFollower).
    //Add clips that move from startT → endT over each clip’s duration. Use the curve for ease-in/out, holds, etc.
    public class FPSplineFollower : MonoBehaviour
    {
        [Header("Path")]
        public SplineContainer TheSpline;
        public Transform TheTarget;
        [Tooltip("If you're using the command to pass animations")]
        public Animator TheAnimator;
        [Range(0f, 1f)] public float t = 0f;
        [Tooltip("If true, Timeline drives t via SetNormalizedT. If false, Update() advances t.")]
        public bool TimelineControl;

        [Header("Motion")]
        [Tooltip("Multiplier applied when clips drive t; useful for slow/fast motion.")]
        public float SpeedMultiplier = 1f;  // affects Timeline-driven movement
        public float NormalizedSpeedPerSecond = 0.2f; // used only when TimelineControl == false
        [Space]
        public bool PingPongSpeed = false;
        [Range(0,1f)]
        [SerializeField]protected float pingPongValue = 0.02f;
        [Space]
        public bool OrientToTangent = true;
        [Tooltip("If true, rotate to spline tangent but remove pitch (yaw-only) using TransformUp as the 'up' axis.")]
        public bool OrientToTangentYawOnly = false;
        
        [SerializeField, Tooltip("Used when tangent is nearly vertical and yaw-only forward would be zero.")]
        protected Vector3 _lastValidYawForward = Vector3.forward;

        public Vector3 TransformUp = Vector3.up;
        [Tooltip("Set this to true if you want to continue to walk the spline")]
        public bool LoopSpline = false;
        //Current runtime state
        public bool IsPaused => _paused;
        public bool IsStopped => _stopped;
        //When true, preview commands (e.g., from Timeline) will bypass pause/stop gates 
        public bool ForcePreviewIgnoreGates { get; set; }
        protected bool _paused;
        protected bool _stopped;


        public virtual void HotSplineSwap(SplineContainer newContainer, float splineStartValue)
        {
            _paused = true;
            TheSpline = newContainer;
            t = Mathf.Clamp01(splineStartValue);
            UpdateTransform();
            _paused = false;
        }
        public virtual void Update()
        {
            // If you also want free-run (not Timeline-driven), you can add optional logic here.
            // This follower is primarily driven by Timeline (SetNormalizedT).
            if (TimelineControl)
            {
                return;
            }
            // Free-run mode: we own advancing t here.
            if (_stopped || _paused) return;
            float addPingPong = 0;
            if (PingPongSpeed)
            {
                addPingPong = Mathf.PingPong(Time.time, pingPongValue);
                t += NormalizedSpeedPerSecond * (SpeedMultiplier+addPingPong) * Time.deltaTime;
            }
            else
            {
                t += NormalizedSpeedPerSecond * SpeedMultiplier * Time.deltaTime;
            }
            t = Mathf.Clamp01(t); // or Mathf.Repeat for looping
            if (t >= 1 && LoopSpline)
            {
                t = 0;
            }
            UpdateTransform();
        }

        public void SetNormalizedT(float value)
        {
            // If forced preview override is active, ignore pause/stop gates
            if (!ForcePreviewIgnoreGates && (_stopped || _paused))
                return;

            t = Mathf.Clamp01(value);
            UpdateTransform();
        }

        public void WarpToNormalizedT(float value)  // ignores pause/stop; for teleports
        {
            t = Mathf.Clamp01(value);
            UpdateTransform();
        }

        public virtual void SetPaused(bool paused) => _paused = paused;
        public virtual void Pause(AnimationClip clip, bool returnToClip)
        {
            _paused = true;
            if (clip != null) { PassAnimationClip(clip, returnToClip); }
            
        }
        /// <summary>
        /// If we have a clip were going to use injection to do something
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="returnToClip"></param>
        /// <param name="stopInjection"></param>
        public virtual void Resume(AnimationClip clip, bool returnToClip, bool stopInjection)
        {
            _paused = false;
            if(clip!=null && stopInjection == true)
            {
                Debug.LogError($"We shouldn't be stopping an injection and passing a clip, just pass the clip (injection will automatically stop old one and start passed one");
                return;
            }
            if (clip != null) 
            { 
                PassAnimationClip(clip, returnToClip);
            }
            else if(stopInjection)
            {
                ResumeAnimationClip();
            }
        }
        public virtual void Stop(AnimationClip clip, bool returnToClip)
        {
            _stopped = true;
            if (clip != null) { PassAnimationClip(clip, returnToClip); }
        }
        public virtual void Unstop() => _stopped = false;
        public virtual void SetSpeedMultiplier(float m) => SpeedMultiplier = Mathf.Max(0f, m);

        protected virtual void PassAnimationClip(AnimationClip clip,bool returnToOriginalClip)
        {
            if (TheAnimator != null)
            {
                if (TheAnimator.GetComponent<FPAnimationInjector>())
                {
                    TheAnimator.GetComponent<FPAnimationInjector>().PlayClip(clip,fadeIn:-1,fadeOut:-1,targetWeight:-1,mask:null,additive:false,speed:1, returnToOriginalClip);
                }
            }
        }
        /// <summary>
        /// If we have an FPAnimationInjector and it happens to be active or running somethin
        /// </summary>
        protected virtual void ResumeAnimationClip()
        {
            if (TheAnimator != null)
            {
                if (TheAnimator.GetComponent<FPAnimationInjector>())
                {
                    TheAnimator.GetComponent<FPAnimationInjector>().StopActiveInjection();
                }
            }
        }
        protected void UpdateTransform()
        {
            if (TheSpline == null || TheSpline.Spline == null || TheTarget==null) return;

            var sp = TheSpline.Spline;

            Vector3 localPos = (Vector3) sp.EvaluatePosition(t);
            Vector3 worldPos = TheSpline.transform.TransformPoint(localPos);
            TheTarget.position = worldPos;

            if (OrientToTangent)
            {
                Vector3 localTan = (Vector3)sp.EvaluateTangent(t);
                Vector3 worldTan = TheSpline.transform.TransformVector(localTan);

                if (worldTan.sqrMagnitude > 1e-6f)
                {
                    Vector3 worldUp = TheSpline.transform.TransformDirection(TransformUp);

                    Vector3 forward = worldTan;

                    if (OrientToTangentYawOnly)
                    {
                        // Remove pitch by flattening the tangent onto the plane defined by 'worldUp'
                        Vector3 flattened = Vector3.ProjectOnPlane(worldTan, worldUp);

                        if (flattened.sqrMagnitude > 1e-6f)
                        {
                            forward = flattened;
                            _lastValidYawForward = flattened.normalized;
                        }
                        else
                        {
                            // Tangent is almost parallel to up (e.g., ladder straight up).
                            // Keep previous yaw forward so rotation doesn't explode / snap.
                            forward = _lastValidYawForward;
                        }
                    }

                    TheTarget.rotation = Quaternion.LookRotation(forward, worldUp);
                }
            }
        }
    }
}
